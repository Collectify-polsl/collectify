using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;
using CCollection = Collectify.Model.Collection.Collection;

namespace Collectify.Data.Services;

/// <summary>
/// EF Core implementation of IItemService.
/// </summary>
public class ItemService : IItemService
{
    private readonly IUnitOfWork _unitOfWork;

    public ItemService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Item> CreateItemAsync(int collectionId, IReadOnlyList<NewItemFieldValueInput> fieldValues, int? previousItemId,
        int? nextItemId, CancellationToken cancellationToken = default)
    {
        CCollection? collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);

        if (collection is null)
            throw new InvalidOperationException($"Collection with id {collectionId} was not found.");

        await ValidateLinksAsync(currentItemId: null, collectionId: collectionId, previousItemId: previousItemId, nextItemId: nextItemId,
            cancellationToken: cancellationToken);

        Item item = new Item
        {
            CollectionId = collectionId,
            CreationDate = DateTime.UtcNow,
            PreviousItemId = previousItemId,
            NextItemId = nextItemId
        };

        List<FieldValue> values = await BuildFieldValuesAsync(item, fieldValues, cancellationToken);

        foreach (FieldValue v in values)
            await _unitOfWork.FieldValues.AddAsync(v, cancellationToken);

        await _unitOfWork.Items.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await SetBidirectionalLinksAsync(item, previousItemId, nextItemId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task<IReadOnlyList<Item>> GetItemsForCollectionAsync(int collectionId, string? search = null, int? sortByFieldDefinitionId = null,
        bool descending = false, CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.Items.GetByCollectionIdAsync(collectionId, cancellationToken);

        IEnumerable<Item> query = items;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(i => i.FieldValues.Any(v =>
                !string.IsNullOrEmpty(v.TextValue) &&
                v.TextValue.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (sortByFieldDefinitionId is null)
        {
            query = descending ? query.OrderByDescending(i => i.CreationDate) : query.OrderBy(i => i.CreationDate);

            return query.ToList();
        }

        FieldDefinition? def = await _unitOfWork.FieldDefinitions.GetByIdAsync(sortByFieldDefinitionId.Value, cancellationToken);

        if (def is null)
            throw new InvalidOperationException($"FieldDefinition with id {sortByFieldDefinitionId.Value} was not found.");

        Func<Item, object?> keySelector = def.FieldType switch
        {
            FieldType.Text => i => i.FieldValues
                .FirstOrDefault(v => v.FieldDefinitionId == def.Id)?.TextValue,

            FieldType.Integer => i => i.FieldValues
                .FirstOrDefault(v => v.FieldDefinitionId == def.Id)?.IntValue,

            FieldType.Decimal => i => i.FieldValues
                .FirstOrDefault(v => v.FieldDefinitionId == def.Id)?.DecimalValue,

            FieldType.Date => i => i.FieldValues
                .FirstOrDefault(v => v.FieldDefinitionId == def.Id)?.DateValue,

            FieldType.ItemReference => i => i.FieldValues
                .FirstOrDefault(v => v.FieldDefinitionId == def.Id)?.RelatedItemId,

            _ => throw new NotSupportedException($"Unsupported type {def.FieldType}")
        };

        query = descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

        return query.ToList();
    }

    public async Task<Item> UpdateItemAsync(int itemId, IReadOnlyList<NewItemFieldValueInput> fieldValues, int? previousItemId, int? nextItemId,
        CancellationToken cancellationToken = default)
    {
        Item? item = await _unitOfWork.Items.GetByIdAsync(itemId, cancellationToken);

        if (item is null)
            throw new InvalidOperationException($"Item with id {itemId} was not found.");

        int collectionId = item.CollectionId;

        int? oldPreviousId = item.PreviousItemId;
        int? oldNextId = item.NextItemId;

        await ValidateLinksAsync(currentItemId: itemId, collectionId: collectionId, previousItemId: previousItemId, nextItemId: nextItemId,
            cancellationToken: cancellationToken);

        if (oldPreviousId.HasValue && oldPreviousId != previousItemId)
        {
            Item? oldPrev = await _unitOfWork.Items.GetByIdAsync(oldPreviousId.Value, cancellationToken);

            if (oldPrev is not null && oldPrev.NextItemId == itemId)
            {
                oldPrev.NextItemId = null;
                _unitOfWork.Items.Update(oldPrev);
            }
        }

        if (oldNextId.HasValue && oldNextId != nextItemId)
        {
            Item? oldNext = await _unitOfWork.Items.GetByIdAsync(oldNextId.Value, cancellationToken);

            if (oldNext is not null && oldNext.PreviousItemId == itemId)
            {
                oldNext.PreviousItemId = null;
                _unitOfWork.Items.Update(oldNext);
            }
        }

        item.PreviousItemId = previousItemId;
        item.NextItemId = nextItemId;

        IReadOnlyList<FieldValue> existingValues = await _unitOfWork.FieldValues.FindAsync(v => v.ItemId == itemId, cancellationToken);

        foreach (FieldValue v in existingValues)
            _unitOfWork.FieldValues.Remove(v);

        List<FieldValue> newValues = await BuildFieldValuesAsync(item, fieldValues, cancellationToken);

        foreach (FieldValue v in newValues)
            await _unitOfWork.FieldValues.AddAsync(v, cancellationToken);

        _unitOfWork.Items.Update(item);
        await SetBidirectionalLinksAsync(item, previousItemId, nextItemId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task DeleteItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        Item? item = await _unitOfWork.Items.GetByIdAsync(itemId, cancellationToken);

        if (item is null)
            return;

        int? prevId = item.PreviousItemId;
        int? nextId = item.NextItemId;

        if (prevId.HasValue)
        {
            Item? prev = await _unitOfWork.Items.GetByIdAsync(prevId.Value, cancellationToken);

            if (prev is not null && prev.NextItemId == itemId)
            {
                prev.NextItemId = null;
                _unitOfWork.Items.Update(prev);
            }
        }

        if (nextId.HasValue)
        {
            Item? next = await _unitOfWork.Items.GetByIdAsync(nextId.Value, cancellationToken);

            if (next is not null && next.PreviousItemId == itemId)
            {
                next.PreviousItemId = null;
                _unitOfWork.Items.Update(next);
            }
        }

        _unitOfWork.Items.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Builds a list of field values for the specified item based on the provided input values and field definitions.
    /// </summary>
    /// <param name="item">The item to which the field values will be associated.</param>
    /// <param name="inputs">A read-only list of input values specifying the field definitions and corresponding values to assign. Each input
    /// must reference a valid field definition.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A list of field values constructed for the specified item, with each value corresponding to an input and its
    /// associated field definition.</returns>
    /// <exception cref="InvalidOperationException">Thrown if an input references a field definition that does not exist.</exception>
    /// <exception cref="NotSupportedException">Thrown if an input references a field type that is not supported.</exception>
    private async Task<List<FieldValue>> BuildFieldValuesAsync(Item item, IReadOnlyList<NewItemFieldValueInput> inputs, CancellationToken cancellationToken)
    {
        int[] definitionIds = inputs
            .Select(v => v.FieldDefinitionId)
            .Distinct()
            .ToArray();

        IReadOnlyList<FieldDefinition> definitions = await _unitOfWork.FieldDefinitions.FindAsync(d => definitionIds.Contains(d.Id),
            cancellationToken);

        Dictionary<int, FieldDefinition> definitionsById = definitions.ToDictionary(d => d.Id);

        List<FieldValue> result = new List<FieldValue>();

        foreach (NewItemFieldValueInput input in inputs)
        {
            if (!definitionsById.TryGetValue(input.FieldDefinitionId, out FieldDefinition? def))
                throw new InvalidOperationException($"FieldDefinition with id {input.FieldDefinitionId} was not found.");

            FieldValue fv = new FieldValue
            {
                Item = item,
                FieldDefinitionId = def.Id
            };

            switch (def.FieldType)
            {
                case FieldType.Text:
                    fv.TextValue = input.TextValue;
                    break;

                case FieldType.Integer:
                    if (input.IntValue == null && !string.IsNullOrEmpty(input.TextValue))
                        throw new ArgumentException($"Field '{def.Name}' requires an integer value.");
                    fv.IntValue = input.IntValue;
                    break;

                case FieldType.Decimal:
                    if (input.DecimalValue == null && input.IntValue != null)
                        fv.DecimalValue = Convert.ToDecimal(input.IntValue);
                    else if (input.DecimalValue == null && !string.IsNullOrEmpty(input.TextValue))
                        throw new ArgumentException($"Field '{def.Name}' requires a decimal value.");
                    else
                        fv.DecimalValue = input.DecimalValue;
                    break;

                case FieldType.Date:
                    if (input.DateValue == null && !string.IsNullOrEmpty(input.TextValue))
                        throw new ArgumentException($"Field '{def.Name}' requires a valid date.");
                    fv.DateValue = input.DateValue;
                    break;

                case FieldType.ItemReference:
                    fv.RelatedItemId = input.RelatedItemId;
                    break;

                case FieldType.Image:
                    fv.ImageValue = input.ImageValue;
                    break;

                default:
                    throw new NotSupportedException($"Unsupported FieldType {def.FieldType}.");
            }

            result.Add(fv);
        }

        return result;
    }

    /// <summary>
    /// Validates that the specified previous and next item links are consistent and belong to the given collection.
    /// </summary>
    /// <param name="currentItemId">The identifier of the current item being validated, or null if the item does not exist yet.</param>
    /// <param name="collectionId">The identifier of the collection to which the items must belong.</param>
    /// <param name="previousItemId">The identifier of the item expected to precede the current item, or null if there is no previous item.</param>
    /// <param name="nextItemId">The identifier of the item expected to follow the current item, or null if there is no next item.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified previous or next item does not exist, does not belong to the collection, or if the link
    /// relationships are inconsistent.</exception>
    private async Task ValidateLinksAsync(int? currentItemId, int collectionId, int? previousItemId, int? nextItemId,
        CancellationToken cancellationToken)
    {
        if (currentItemId.HasValue)
        {
            if (previousItemId.HasValue && previousItemId.Value == currentItemId.Value)
                throw new InvalidOperationException("Item cannot have itself as previous item.");

            if (nextItemId.HasValue && nextItemId.Value == currentItemId.Value)
                throw new InvalidOperationException("Item cannot have itself as next item.");
        }

        if (previousItemId.HasValue)
        {
            Item? prev = await _unitOfWork.Items.GetByIdAsync(previousItemId.Value, cancellationToken);

            if (prev is null || prev.CollectionId != collectionId)
                throw new InvalidOperationException("Previous item must exist and belong to the same collection.");

            if (prev.NextItemId.HasValue && (!currentItemId.HasValue || prev.NextItemId.Value != currentItemId.Value))
                throw new InvalidOperationException("Previous item already has a different next item assigned.");
        }

        if (nextItemId.HasValue)
        {
            Item? next = await _unitOfWork.Items.GetByIdAsync(nextItemId.Value, cancellationToken);

            if (next is null || next.CollectionId != collectionId)
                throw new InvalidOperationException("Next item must exist and belong to the same collection.");

            if (next.PreviousItemId.HasValue && (!currentItemId.HasValue || next.PreviousItemId.Value != currentItemId.Value))
            {
                throw new InvalidOperationException("Next item already has a different previous item assigned.");
            }
        }
    }

    /// <summary>
    /// Asynchronously updates the previous and next item links to establish bidirectional relationships with the specified item.
    /// </summary>
    /// <param name="item">The item for which to set bidirectional links. The item's Id is used to update adjacent items.</param>
    /// <param name="previousItemId">The identifier of the previous item to link to, or null if there is no previous item. If specified, the previous
    /// item's next link will be updated to point to the given item.</param>
    /// <param name="nextItemId">The identifier of the next item to link to, or null if there is no next item. If specified, the next item's
    /// previous link will be updated to point to the given item.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SetBidirectionalLinksAsync(Item item, int? previousItemId, int? nextItemId, CancellationToken cancellationToken)
    {
        if (previousItemId.HasValue)
        {
            Item? prev = await _unitOfWork.Items.GetByIdAsync(previousItemId.Value, cancellationToken);

            if (prev is not null)
            {
                prev.NextItemId = item.Id;
                _unitOfWork.Items.Update(prev);
            }
        }

        if (nextItemId.HasValue)
        {
            Item? next = await _unitOfWork.Items.GetByIdAsync(nextItemId.Value, cancellationToken);

            if (next is not null)
            {
                next.PreviousItemId = item.Id;
                _unitOfWork.Items.Update(next);
            }
        }
    }
}
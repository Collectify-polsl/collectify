using CCollection = Collectify.Model.Collection.Collection;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;

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

    public async Task<Item> CreateItemAsync(int collectionId, IReadOnlyList<NewItemFieldValueInput> fieldValues, 
        CancellationToken cancellationToken = default)
    {
        CCollection? collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);

        if (collection is null)
            throw new InvalidOperationException($"Collection with id {collectionId} was not found.");

        Item item = new Item
        {
            CollectionId = collectionId,
            CreationDate = DateTime.UtcNow
        };

        List<FieldValue> values = await BuildFieldValuesAsync(item, fieldValues, cancellationToken);

        foreach (var v in values)
            await _unitOfWork.FieldValues.AddAsync(v, cancellationToken);

        await _unitOfWork.Items.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task<IReadOnlyList<Item>> GetItemsForCollectionAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Items.GetByCollectionIdAsync(collectionId, cancellationToken);
    }

    public async Task DeleteItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        Item? item = await _unitOfWork.Items.GetByIdAsync(itemId, cancellationToken);

        if (item is null)
            return;

        _unitOfWork.Items.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Item> UpdateItemAsync(int itemId, IReadOnlyList<NewItemFieldValueInput> fieldValues, 
        CancellationToken cancellationToken = default)
    {
        Item? item = await _unitOfWork.Items.GetByIdAsync(itemId, cancellationToken);

        if (item is null)
            throw new InvalidOperationException($"Item with id {itemId} was not found.");

        var existingValues = await _unitOfWork.FieldValues
            .FindAsync(v => v.ItemId == itemId, cancellationToken);

        foreach (var v in existingValues)
            _unitOfWork.FieldValues.Remove(v);

        List<FieldValue> newValues = await BuildFieldValuesAsync(item, fieldValues, cancellationToken);

        foreach (var v in newValues)
            await _unitOfWork.FieldValues.AddAsync(v, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return item;
    }

    private async Task<List<FieldValue>> BuildFieldValuesAsync(Item item, IReadOnlyList<NewItemFieldValueInput> inputs, 
        CancellationToken cancellationToken)
    {
        int[] definitionIds = inputs
            .Select(v => v.FieldDefinitionId)
            .Distinct()
            .ToArray();

        IReadOnlyList<FieldDefinition> definitions = await _unitOfWork.FieldDefinitions.FindAsync(
            d => definitionIds.Contains(d.Id), cancellationToken);

        var definitionsById = definitions.ToDictionary(d => d.Id);

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
                    fv.IntValue = input.IntValue;
                    break;

                case FieldType.Decimal:
                    fv.DecimalValue = input.DecimalValue;
                    break;

                case FieldType.Date:
                    fv.DateValue = input.DateValue;
                    break;

                case FieldType.ItemReference:
                    fv.RelatedItemId = input.RelatedItemId;
                    break;

                default:
                    throw new NotSupportedException($"Unsupported FieldType {def.FieldType}.");
            }

            result.Add(fv);
        }

        return result;
    }
}
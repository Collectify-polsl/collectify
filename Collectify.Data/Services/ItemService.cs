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

        int[] definitionIds = fieldValues
            .Select(v => v.FieldDefinitionId)
            .Distinct()
            .ToArray();

        IReadOnlyList<FieldDefinition> definitions = 
            await _unitOfWork.FieldDefinitions.FindAsync(d => definitionIds.Contains(d.Id),cancellationToken);

        Dictionary<int, FieldDefinition> definitionsById = definitions.ToDictionary(d => d.Id);

        foreach (NewItemFieldValueInput valueInput in fieldValues)
        {
            if (!definitionsById.TryGetValue(valueInput.FieldDefinitionId, out FieldDefinition? definition))
                throw new InvalidOperationException($"FieldDefinition with id {valueInput.FieldDefinitionId} was not found.");

            FieldValue fieldValue = new FieldValue
            {
                Item = item,
                FieldDefinitionId = definition.Id
            };

            switch (definition.FieldType)
            {
                case FieldType.Text:
                    fieldValue.TextValue = valueInput.TextValue;
                    break;

                case FieldType.Integer:
                    fieldValue.IntValue = valueInput.IntValue;
                    break;

                case FieldType.Decimal:
                    fieldValue.DecimalValue = valueInput.DecimalValue;
                    break;

                case FieldType.Date:
                    fieldValue.DateValue = valueInput.DateValue;
                    break;

                case FieldType.ItemReference:
                    fieldValue.RelatedItemId = valueInput.RelatedItemId;
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported FieldType {definition.FieldType} in ItemService.");
            }

            await _unitOfWork.FieldValues.AddAsync(fieldValue, cancellationToken);
        }

        await _unitOfWork.Items.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task<IReadOnlyList<Item>> GetItemsForCollectionAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Items.GetByCollectionIdAsync(collectionId, cancellationToken);
    }
}
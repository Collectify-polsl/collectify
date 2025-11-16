namespace Collectify.Data;

using Collectify.Model.Collection;
using Collectify.Model.Entities;
using System.Linq;

public class ItemService(CollectifyContext context)
{
    private readonly CollectifyContext _context = context;

    public void SetFieldValue(Item item, FieldDefinition fieldDef, object? value)
    {
        if (fieldDef.IsList)
        {
            var newFieldValue = new FieldValue
            {
                Item = item,
                ItemId = item.Id,
                FieldDefinition = fieldDef,
                FieldDefinitionId = fieldDef.Id
            };
            newFieldValue.SetValue(value);

            if (item.Id > 0)
            {
                _context.Values.Add(newFieldValue);
            }
            else
            {
                item.FieldValues.Add(newFieldValue);
            }
        }
        else
        {
            var fieldValue = item.FieldValues
                .FirstOrDefault(x => x.FieldDefinitionId == fieldDef.Id);

            if (fieldValue == null)
            {
                fieldValue = new FieldValue
                {
                    Item = item,
                    ItemId = item.Id,
                    FieldDefinition = fieldDef,
                    FieldDefinitionId = fieldDef.Id
                };

                if (item.Id > 0)
                {
                    _context.Values.Add(fieldValue);
                }
                else
                {
                    item.FieldValues.Add(fieldValue);
                }
            }

            fieldValue.SetValue(value);
        }
    }

    public bool RemoveValueFromFieldList(Item item, FieldDefinition fieldDef, object? valueToRemove)
    {
        if (!fieldDef.IsList)
        {
            return false;
        }

        var fieldValueToRemove = item.FieldValues
            .Where(x => x.FieldDefinitionId == fieldDef.Id)
            .FirstOrDefault(x =>
            {
                var val = x.GetValue();
                return (val == null && valueToRemove == null) || (val != null && val.Equals(valueToRemove));
            });

        if (fieldValueToRemove != null)
        {
            _context.Values.Remove(fieldValueToRemove);
            item.FieldValues.Remove(fieldValueToRemove);
            return true;
        }

        return false;
    }

    public List<object?> GetFieldValues(Item item, FieldDefinition fieldDef)
    {
        if (!item.FieldValues.Any() && item.Id > 0)
        {
            _context.Entry(item)
               .Collection(x => x.FieldValues)
               .Load();
        }

        return item.FieldValues
            .Where(x => x.FieldDefinitionId == fieldDef.Id)
            .Select(x => x.GetValue())
            .ToList();
    }
}
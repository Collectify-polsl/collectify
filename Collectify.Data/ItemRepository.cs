using Collectify.Model.Collection;
using Collectify.Model.Entities;
using System.Linq;

public class ItemService
{
    public void SetFieldValue(Item item, FieldDefinition fieldDef, object? value)
    {
        if (fieldDef.IsList)
        {
            var newFieldValue = new FieldValue
            {
                Item = item,
                FieldDefinition = fieldDef,
            };
            newFieldValue.SetValue(value);
            item.FieldValues.Add(newFieldValue);
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
                    FieldDefinition = fieldDef,
                };
                item.FieldValues.Add(fieldValue);
            }

            fieldValue.SetValue(value);
        }
    }

    public bool RemoveValueFromFieldList(Item item, FieldDefinition fieldDef, object? valueToRemove)
    {
        if (!fieldDef.IsList)
            return false;

        var fieldValueToRemove = item.FieldValues
            .Where(x => x.FieldDefinitionId == fieldDef.Id)
            .FirstOrDefault(x =>
            {
                var val = x.GetValue();
                return (val == null && valueToRemove == null) || (val != null && val.Equals(valueToRemove));
            });

        if (fieldValueToRemove != null)
        {
            item.FieldValues.Remove(fieldValueToRemove);
            return true;
        }

        return false;
    }

    public List<object?> GetFieldValues(Item item, FieldDefinition fieldDef)
    {
        return item.FieldValues
            .Where(x => x.FieldDefinitionId == fieldDef.Id)
            .Select(x => x.GetValue())
            .ToList();
    }
}
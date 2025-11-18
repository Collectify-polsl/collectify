using Collectify.Model.Collection;
using Collectify.Model.Entities;

/// <summary>
/// Service that provides helper methods for manipulating field values on items. Handles both single value fields and list type fields.
/// </summary>
public class ItemService
{
    /// <summary>
    /// Sets a value for a given field on the specified item.
    /// For list fields a new field value is always added. For non list fields the existing value is updated or a new one is created.
    /// </summary>
    /// <param name="item">Item whose field value should be set.</param>
    /// <param name="fieldDef">Field definition that describes the field.</param>
    /// <param name="value">Value to assign to the field.</param>
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

    /// <summary>
    /// Removes a single matching value from a list type field on the given item.
    /// </summary>
    /// <param name="item">Item whose field value should be removed.</param>
    /// <param name="fieldDef">Field definition that must represent a list field.</param>
    /// <param name="valueToRemove">Value to be removed from the field list.</param>
    /// <returns>
    /// True if a matching value existed and was removed, false if the field is not a list or no matching value was found.
    /// </returns>
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

    /// <summary>
    /// Retrieves all values for a given field on the specified item. 
    /// For list fields all list elements are returned, for non list fields this is at most a single value.
    /// </summary>
    /// <param name="item">Item whose field values should be retrieved.</param>
    /// <param name="fieldDef">Field definition that identifies the field.</param>
    /// <returns>List of values stored for the given field on the item.</returns>
    public List<object?> GetFieldValues(Item item, FieldDefinition fieldDef)
    {
        return item.FieldValues
            .Where(x => x.FieldDefinitionId == fieldDef.Id)
            .Select(x => x.GetValue())
            .ToList();
    }
}
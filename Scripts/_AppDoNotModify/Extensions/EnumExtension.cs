using System.Reflection;
using System;
using System.ComponentModel;
using System.Linq; 
using System.Collections.Generic;
using Test._Definitions;

namespace Test._ScriptExtensions
{
    public static class EnumExtension
    {
        public static string GetDescription(this Enum value)
        {
            string description = string.Empty;
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field != null)
            {
                var attr = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
                if (attr != null)
                {
                    description = attr.Description;
                }
            }
            return description;
        }

        public static List<string> GetTags(this Enum value)
        {
            List<string> tags = new List<string>();
            FieldInfo field = value.GetType().GetField(value.ToString());

            if (field != null)
            {
                var attributes = Attribute.GetCustomAttributes(field, typeof(TagAttribute));
                if (attributes != null)
                {
                    foreach (TagAttribute attr in attributes)
                    {
                        tags.Add(attr.Tag);
                    }
                }
            }
            return tags;
        }

        public static string GetTag(this Enum value)
        {
            string tag = string.Empty;
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field != null)
            {
                var attributes = Attribute.GetCustomAttributes(field, typeof(TagAttribute));
                if (attributes != null && attributes.Length > 0)
                {
                    tag = ((TagAttribute)attributes[0]).Tag;
                }
            }
            return tag;
        }

        public static bool HasTag(this Enum value)
        {
            bool result = false;
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field != null)
            { 
                var attributes = Attribute.GetCustomAttributes(field, typeof(TagAttribute));
                if (attributes != null && attributes.Length > 0)
                {
                    result = true;
                }
            }
            return result;
        }

        public static bool HasTag(this PropertyInfo value)
        {
            bool result = false; 
            if (value != null)
            {
                var attributes = Attribute.GetCustomAttributes(value, typeof(TagAttribute)); 
                if (attributes != null && attributes.Length > 0)
                {
                    result = true;
                }
            }
            return result;
        }
    }
}

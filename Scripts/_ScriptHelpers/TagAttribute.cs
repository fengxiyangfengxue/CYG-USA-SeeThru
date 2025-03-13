using System;

namespace Test._ScriptHelpers
{
    public class TagAttribute: Attribute
    {
        public static readonly TagAttribute Default = new TagAttribute();
        private string description;
        public virtual string Description => DescriptionValue;

        protected string DescriptionValue
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
            }
        }
        public TagAttribute()
           : this(string.Empty)
        {
        }
        public TagAttribute(string description)
        {
            this.description = description;
        }
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            TagAttribute descriptionAttribute = obj as TagAttribute;
            if (descriptionAttribute != null)
            {
                return descriptionAttribute.Description == Description;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Description.GetHashCode();
        }

        public override bool IsDefaultAttribute()
        {
            return Equals(Default);
        }
    }
}

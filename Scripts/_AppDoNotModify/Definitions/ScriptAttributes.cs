using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Definition;

namespace Test._Definitions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class MainClassConstructorAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public MainClassConstructorAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }



    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class BeforeTestingAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public BeforeTestingAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class TriggerAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public TriggerAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class ScriptInitializeAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public ScriptInitializeAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }




    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class AfterScriptAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public AfterScriptAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class BeforeSavingLogAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public BeforeSavingLogAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class LogFilterAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public LogFilterAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }



    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class AfterSavingLogAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public AfterSavingLogAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class BeforeShowingResultAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public BeforeShowingResultAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class AfterTestingAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public AfterTestingAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class AfterClosedAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public AfterClosedAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class MainClassDisposeAttribute : Attribute
    {
        public TEST_STATION Station { get; set; }
        public int Level { get; set; }
        public MainClassDisposeAttribute(TEST_STATION station, int level = 0)
        {
            Station = station;
            Level = level;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class XmlFileNameAttribute : Attribute
    {
        public string FileName { get; set; }
        public XmlFileNameAttribute(string fileName)
        {
            FileName = fileName;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class JsonFileNameAttribute : Attribute
    {
        public string FileName { get; set; }
        public JsonFileNameAttribute(string fileName)
        {
            FileName = fileName;
        }
    }


    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class TagAttribute : Attribute
    {
        string _tag;

        public string Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        public TagAttribute() : this(string.Empty)
        {

        }

        public TagAttribute(string description)
        {
            _tag = description;
        }

        public override int GetHashCode()
        {
            return Tag.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            TagAttribute attr = obj as TagAttribute;
            return attr != null && attr.Tag == Tag;
        }
    }
}

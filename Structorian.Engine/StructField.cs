using Structorian.Engine.Fields;
using System;
using System.Collections.Generic;
using System.IO;

namespace Structorian.Engine
{
    public abstract class StructField
    {
        protected StructDef _structDef;
        private List<StructField> _childFields = null;
        private StructField _linkedToField = null;
        protected List<StructField> _linkedFields = null;
        private string _tag;
        private string _id;
        private string _defaultAttribute = "tag";
        private bool _acceptsChildren = false;
        protected bool _hidden;
        private long _offset;
        private Dictionary<string, object> _attributeValues = new Dictionary<string, object>();

        protected StructField(StructDef structDef)
        {
            _structDef = structDef;
        }

        protected StructField(StructDef structDef, string defaultAttribute, bool acceptsChildren)
        {
            _structDef = structDef;
            _defaultAttribute = defaultAttribute;
            _acceptsChildren = acceptsChildren;
        }

        public StructDef StructDef
        {
            get { return _structDef; }
        }

        public string Id
        {
            get
            {
                if (_id != null) return _id;
                return _tag;
            }
        }

        public string Tag
        {
            get { return _tag; }
        }

        public TextPosition Position { get; set; }
        public string Name { get; set; }
        public TextPosition EndPosition { get; set; }

        public virtual void SetAttribute(string key, string value)
        {
            if (key == "tag")
                _tag = value;
            else if (key == "id")
                _id = value;
            else if (key == "hidden")
                _hidden = (Int32.Parse(value) > 0);
            else
                throw new Exception("Unknown attribute " + key);
        }
        
        internal void SetAttributeValue(string key, object value)
        {
            _attributeValues[key] = value;
        }
        
        public object GetAttribute(string key)
        {
            object result;
            if (!_attributeValues.TryGetValue(key, out result))
                return null;
            return result;
        }
        
        public Expression GetExpressionAttribute(string key)
        {
            return (Expression) GetAttribute(key);
        }

        public string GetStringAttribute(string key)
        {
            return (string) GetAttribute(key);
        }

        public int? GetIntAttribute(string key)
        {
            return (int?)GetAttribute(key);
        }

        public bool GetBoolAttribute(string key)
        {
            object attr = GetAttribute(key);
            if (attr == null) return false;
            return (bool) attr;
        }
        
        public StructDef GetStructAttribute(string key)
        {
            return (StructDef) GetAttribute(key);
        }

        public EnumDef GetEnumAttribute(string key)
        {
            return (EnumDef)GetAttribute(key);
        }

        public virtual string DefaultAttribute
        {
            get { return _defaultAttribute;  }
        }

        public List<StructField> ChildFields
        {
            get
            {   
                if (_childFields == null)
                    _childFields = new List<StructField>();
                return _childFields;
            }
        }
        
        public virtual void Validate()
        {
        }

        public abstract void LoadData(BinaryReader reader, StructInstance instance);

        public void AddChildField(StructField field)
        {
            if (!_acceptsChildren)
                throw new Exception("Field does not accept children");
            
            if (_childFields == null)
                _childFields = new List<StructField>();
            _childFields.Add(field);
        }

        protected void LoadChildFields(BinaryReader reader, StructInstance instance)
        {
            foreach (StructField field in ChildFields)
            {
                if (!field.IsLinked)
                    field.LoadData(reader, instance);
            }
        }
        
        public virtual bool ProvidesChildren()
        {
            foreach(StructField field in ChildFields)
            {
                if (field.ProvidesChildren()) return true;
            }
            return false;
        }

        protected StructCell AddCell(StructInstance instance, IConvertible value, int offset)
        {
            StructCell cell = new ValueCell(this, value, offset);
            instance.AddCell(cell, _hidden);
            return cell;
        }

        protected void AddCell(StructInstance instance, IConvertible value, string displayValue, int offset)
        {
            StructCell cell = new ValueCell(this, value, displayValue, offset);
            instance.AddCell(cell, _hidden);
        }

        protected void AddCell(StructInstance instance, Expression expr)
        {
            instance.AddCell(new ExprCell(this, instance, expr), _hidden);
        }
        
        protected internal virtual bool CanLinkField(StructField nextField)
        {
            return false;
        }

        internal void LinkField(StructField field)
        {
            if (_linkedFields == null)
                _linkedFields = new List<StructField>();
            _linkedFields.Add(field);
            field._linkedToField = this;
        }
        
        internal bool IsLinked
        {
            get { return _linkedToField != null; }
        }

        public long Offset { get => _offset; set => _offset = value; }

        public virtual int GetDataSize()
        {
            return 0;
        }
        
        public virtual int GetInstanceDataSize(StructCell cell, StructInstance instance)
        {
            int? cellSize = instance.GetCellSize(cell);
            if (cellSize.HasValue)
                return cellSize.Value;
            return GetDataSize();
        }

        public static bool IsNumericType(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}

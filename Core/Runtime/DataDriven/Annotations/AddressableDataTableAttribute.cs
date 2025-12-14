using System;

namespace Chris.DataDriven
{
    /// <summary>
    /// Auto manage dataTable with this row to Addressables
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AddressableDataTableAttribute : Attribute
    {
        /// <summary>
        /// Asset group DataTable belongs to
        /// </summary>
        public string Group { get; }
        
        public string Address { get; }

        public AddressableDataTableAttribute(string group = "DataTables" /* Default group for DataTables */, 
            string address = null)
        {
            Group = group;
            Address = address;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Chris.Resource.Editor
{
    /// <summary>
    /// Searchable dropdown (similar to Unity's "Add Component") used to pick an
    /// <see cref="AddressableAssetGroup"/> to add into a <see cref="RemoteContentProfile"/>.
    /// Only the groups passed to the constructor are listed, so callers can filter out
    /// groups that are already added or read-only.
    /// </summary>
    public sealed class RemoteContentGroupDropdown : AdvancedDropdown
    {
        private readonly List<AddressableAssetGroup> _groups;

        private readonly Action<AddressableAssetGroup> _onSelected;

        public RemoteContentGroupDropdown(AdvancedDropdownState state, IEnumerable<AddressableAssetGroup> groups, Action<AddressableAssetGroup> onSelected)
            : base(state)
        {
            _groups = new List<AddressableAssetGroup>();
            foreach (var group in groups)
            {
                if (group)
                {
                    _groups.Add(group);
                }
            }

            _onSelected = onSelected;
            minimumSize = new Vector2(260, 320);
        }

        public RemoteContentGroupDropdown(IEnumerable<AddressableAssetGroup> groups, Action<AddressableAssetGroup> onSelected)
            : this(new AdvancedDropdownState(), groups, onSelected)
        {
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Addressable Groups");
            if (_groups.Count == 0)
            {
                root.AddChild(new AdvancedDropdownItem("No available groups") { enabled = false });
                return root;
            }

            _groups.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            for (int i = 0; i < _groups.Count; i++)
            {
                root.AddChild(new GroupDropdownItem(_groups[i]));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is not GroupDropdownItem groupItem)
            {
                return;
            }

            _onSelected?.Invoke(groupItem.Group);
        }

        private sealed class GroupDropdownItem : AdvancedDropdownItem
        {
            public GroupDropdownItem(AddressableAssetGroup group)
                : base(group.Name)
            {
                Group = group;
            }

            public AddressableAssetGroup Group { get; }
        }
    }
}

using System;
using System.Windows.Forms;
using Styx;
using Styx.CommonBot.Profiles;

namespace Levelbot.ProfileCreation;

public class ProfileVendorListViewItem : ListViewItem, IEquatable<ProfileVendorListViewItem>
{
	public string VendorName;

	public uint Entry;

	public Vendor.VendorType Type;

	public WoWPoint Location;

	public ProfileVendorListViewItem(string name, uint entry, Vendor.VendorType type, WoWPoint location)
	{
		VendorName = name;
		Entry = entry;
		Type = type;
		Location = location;
		base.Text = name;
	}

	public override string ToString()
	{
		return VendorName;
	}

	public bool Equals(ProfileVendorListViewItem other)
	{
		if (object.ReferenceEquals(null, other))
		{
			return false;
		}
		if (object.ReferenceEquals(this, other))
		{
			return true;
		}
		if (other.Entry != Entry)
		{
			return string.Equals(other.VendorName, VendorName, StringComparison.InvariantCultureIgnoreCase);
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (object.ReferenceEquals(null, obj))
		{
			return false;
		}
		if (object.ReferenceEquals(this, obj))
		{
			return true;
		}
		if (obj.GetType() != typeof(ProfileVendorListViewItem))
		{
			return false;
		}
		return Equals((ProfileVendorListViewItem)obj);
	}

	public override int GetHashCode()
	{
		return (((VendorName != null) ? VendorName.GetHashCode() : 0) * 397) ^ Entry.GetHashCode();
	}

	public static bool operator ==(ProfileVendorListViewItem left, ProfileVendorListViewItem right)
	{
		return object.Equals(left, right);
	}

	public static bool operator !=(ProfileVendorListViewItem left, ProfileVendorListViewItem right)
	{
		return !object.Equals(left, right);
	}
}

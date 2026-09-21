using System.Collections.Generic;
using System.Xml.Linq;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class SetVendorNode : OrderNode
    {
        public SetVendorNode(List<Vendor> vendors) : base(OrderNodeType.SetVendor)
        {
            Vendors = vendors;
        }

        public SetVendorNode(Vendor vendor) : this(new List<Vendor> { vendor })
        {

        }

        public List<Vendor> Vendors { get; private set; }

        public static new SetVendorNode FromXml(XElement element)
        {
            XElement vendorElem = ProfileHelper.FindElement(element, true, "vendor", "vendors");
            if (vendorElem == null)
                throw new ProfileMissingElementException("Vendor", element);

            if (vendorElem.Name.ToString().ToLowerInvariant() == "vendor")
            {
                Vendor vendor;
                try
                {
                    vendor = new Vendor(vendorElem);
                }
                catch (ProfileException ex)
                {
                    throw new ProfileException(StyxResources.Could_not_parse_SetVendor_node_, ex);
                }

                return new SetVendorNode(vendor);
            }

            var vendors = new List<Vendor>();
            foreach (XElement vendorElm in ProfileHelper.FindElements(vendorElem, true, "vendor"))
            {
                try
                {
                    vendors.Add(new Vendor(vendorElm));
                }
                catch (ProfileException ex)
                {
                    throw new ProfileException(StyxResources.Could_not_parse_SetVendor_node_, ex);
                }
            }

            return new SetVendorNode(vendors);
        }

        public override string ToString()
        {
            return string.Format("[SetVendorNode: Vendors: {0}]", string.Join(", ", Vendors.ConvertAll(v => v.ToString()).ToArray()));
        }
    }
}

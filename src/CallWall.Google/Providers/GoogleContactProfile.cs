using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using CallWall.Contract.Contact;
using CallWall.Web;

namespace CallWall.Google.Providers
{
    public sealed class GoogleContactProfile : IContactProfile
    {
        private static readonly XmlNamespaceManager _ns;

        static GoogleContactProfile()
        {
            _ns = new XmlNamespaceManager(new NameTable());
            _ns.AddNamespace("x", "http://www.w3.org/2005/Atom");
            _ns.AddNamespace("openSearch", "http://a9.com/-/spec/opensearch/1.1/");
            _ns.AddNamespace("gContact", "http://schemas.google.com/contact/2008");
            _ns.AddNamespace("batch", "http://schemas.google.com/gdata/batch");
            _ns.AddNamespace("gd", "http://schemas.google.com/g/2005");
        }

        public static GoogleContactProfile Translate(string response, string accessToken)
        {
            var xDoc = XDocument.Parse(response);
            var entryXName = XName.Get("entry", "http://www.w3.org/2005/Atom");

            var xContactEntry = xDoc.Root.Element(entryXName);
            if (xContactEntry == null)
                throw new InvalidOperationException(); //TODO: Support not finding anything.


            var title = XPathString(xContactEntry, "x:title", _ns);
            var fullName = XPathString(xContactEntry, "gd:name/gd:fullName", _ns);
            var dateOfBirth = xContactEntry.Elements(ToXName("gContact", "birthday"))
                                               .Select(x => (DateTime?)x.Attribute("when"))
                                               .FirstOrDefault();

            var avatars = xContactEntry.Elements(ToXName("x", "link"))
                .Where(x => x.Attribute("rel") != null
                            && x.Attribute("rel").Value == "http://schemas.google.com/contacts/2008/rel#photo"
                            && x.Attribute("type") != null
                            && x.Attribute("type").Value == "image/*"
                            && x.Attribute("href") != null)
                .Select(x => x.Attribute("href"))
                .Where(att => att != null)
                .Select(att =>
                {
                    var hrp = new HttpRequestParameters(att.Value);
                    hrp.QueryStringParameters.Add("access_token", accessToken);
                    return hrp.ConstructUri();
                });

            //<gd:email rel='http://schemas.google.com/g/2005#home' address='danrowe1978@gmail.com' primary='true'/>
            var emails = from xElement in xContactEntry.XPathSelectElements("gd:email", _ns)
                         select new ContactAssociation(ToContactAssociation(xElement.Attribute("rel")), xElement.Attribute("address").Value);


            //<gd:phoneNumber rel='http://schemas.google.com/g/2005#mobile' uri='tel:+33-6-43-06-76-58' primary='true'>+33  6 4306 7658</gd:phoneNumber>
            var phoneNumbers = from xElement in xContactEntry.XPathSelectElements("gd:phoneNumber", _ns)
                               select new ContactAssociation(ToContactAssociation(xElement.Attribute("rel")), xElement.Value);

            /*<gd:organization rel='http://schemas.google.com/g/2005#work'><gd:orgName>Technip</gd:orgName></gd:organization>*/
            var organizations = from xElement in xContactEntry.XPathSelectElements("gd:organization", _ns)
                                select new ContactAssociation(ToContactAssociation(xElement.Attribute("rel")), xElement.XPathSelectElement("gd:orgName", _ns).Value);


            //<gContact:relation rel='partner'>Anne</gContact:relation>
            var relationships = from xElement in xContactEntry.XPathSelectElements("gContact:relation", _ns)
                                select new ContactAssociation(ToContactAssociation(xElement.Attribute("rel")), xElement.Value);

            var result = new GoogleContactProfile(title, fullName, dateOfBirth, avatars, emails, phoneNumbers, organizations, relationships);
            return result;
        }

        private static XName ToXName(string prefix, string name)
        {
            var xNamespace = _ns.LookupNamespace(prefix);
            if (xNamespace == null)
                throw new InvalidOperationException(prefix + " namespace prefix is not valid");
            return XName.Get(name, xNamespace);
        }

        private static string XPathString(XNode source, string expression, IXmlNamespaceResolver ns)
        {
            var result = source.XPathSelectElement(expression, ns);
            return result == null ? null : result.Value;
        }

        private static string ToContactAssociation(XAttribute relAttribute)
        {
            //Could be a look
            if (relAttribute == null || relAttribute.Value == null)
                return "Other";
            var hashIndex = relAttribute.Value.LastIndexOf("#", StringComparison.Ordinal);
            var association = relAttribute.Value.Substring(hashIndex + 1);
            return PascalCase(association);
        }

        private static string PascalCase(string input)
        {
            var head = input.Substring(0, 1).ToUpperInvariant();
            var tail = input.Substring(1);
            return head + tail;
        }


        private readonly IEnumerable<Uri> _avatars;
        private readonly IEnumerable<IContactAssociation> _organizations;
        private readonly IEnumerable<IContactAssociation> _relationships;
        private readonly IEnumerable<IContactAssociation> _emailAddresses;
        private readonly IEnumerable<IContactAssociation> _phoneNumbers;

        private GoogleContactProfile(string title, string fullName, DateTime? dateOfBirth, 
            IEnumerable<Uri> avatars,
            IEnumerable<ContactAssociation> emailAddresses, 
            IEnumerable<ContactAssociation> phoneNumbers, 
            IEnumerable<ContactAssociation> organizations, 
            IEnumerable<ContactAssociation> relationships)
        {
            _avatars = avatars;
            _emailAddresses = emailAddresses;
            _phoneNumbers = phoneNumbers;
            _organizations = organizations;
            _relationships = relationships;
            Title = title;
            FullName = fullName;
            DateOfBirth = dateOfBirth;
        }

        /// <summary>
        /// How the user commonly references the contact e.g. Dan Rowe
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// The formal or full name of the contact e.g. Mr. Daniel Alex Rowe
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// The Date of birth for the contact. If the Year is unknown then it should be set to a value of 1.
        /// </summary>
        public DateTime? DateOfBirth { get; private set; }

        /// <summary>
        /// Link to an image or avatar of the contact
        /// </summary>
        public IEnumerable<Uri> Avatars
        {
            get { return _avatars; }
        }
        
        public IEnumerable<IContactAssociation> Organizations
        {
            get { return _organizations; }
        }
        
        public IEnumerable<IContactAssociation> Relationships
        {
            get { return _relationships; }
        }
        
        public IEnumerable<IContactAssociation> EmailAddresses
        {
            get { return _emailAddresses; }
        }
        
        public IEnumerable<IContactAssociation> PhoneNumbers
        {
            get { return _phoneNumbers; }
        }

        private sealed class ContactAssociation : IContactAssociation
        {
            private readonly string _name;
            private readonly string _association;

            public ContactAssociation(string name, string association)
            {
                _name = name;
                _association = association;
            }

            public string Name { get { return _name; } }

            public string Association { get { return _association; } }
        }
    }
}
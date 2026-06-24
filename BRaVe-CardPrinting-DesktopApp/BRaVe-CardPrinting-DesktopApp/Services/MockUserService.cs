using System;
using System.Collections.Generic;
using System.Linq;
using BRaVe_CardPrinting_DesktopApp.Models;

namespace BRaVe_CardPrinting_DesktopApp.Services
{
    public class MockUserService
    {
        private List<User> _users = new List<User>();
        private List<User> _printedUsers = new List<User>();

        //public MockUserService()
        //{
        //    _users = GenerateMockUsers();
        //}

        public List<User> GetAll()
        {
            return _users.Where(u => !u.CardPrinted).ToList();
        }

        public List<User> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return _users;

            keyword = keyword.ToLower();

            return _users.Where(u =>
                
                (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(u.HouseholdId) && u.HouseholdId.ToLower().Contains(keyword))
            ).ToList();
        }

        public List<User> GetPrintedUsers()
        {
            return _printedUsers;
        }

        public void MarkAsPrinted(string id)
        {
            var user = _users.FirstOrDefault(u => u.HouseholdId == id);

            if (user != null)
            {
                // Prevent duplicates
                if (!_printedUsers.Any(u => u.HouseholdId == id))
                {
                    _printedUsers.Add(user);
                }
            }
        }

        //private List<User> GenerateMockUsers()
        //{
        //    return new List<User>
        //    {
               
        //        new User {  FullName = "John Dean Doe", Mission = "IOM South Sudan", HouseholdId = "SOBA0100023", FamilySize = 4, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Health", Activity = "NAV1", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "002", FullName = "Jane Mdoe Smith", Mission = "IOM South Sudan", HouseholdId = "SOBA0100073", FamilySize = 6, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Education", Activity = "NAV2", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "003", FullName = "Ali Moha Hassan", Mission = "IOM South Sudan", HouseholdId = "SOBA0100048", FamilySize = 9, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Shelter", Activity = "NAV3", RegDate = DateTime.Now, CardPrinted = false },

        //        new User { IndividualId = "004", FullName = "Mary Jean Wanjiku", Mission = "IOM South Sudan", HouseholdId = "SOBA0100111", FamilySize = 5, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Health", Activity = "NAV1", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "005", FullName = "Peter Martin Mwangi", Mission = "IOM South Sudan", HouseholdId = "SOBA0100122", FamilySize = 3, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "WASH", Activity = "NAV2", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "006", FullName = "Fatima Ali Noor", Mission = "IOM South Sudan", HouseholdId = "SOBA0100133", FamilySize = 7, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Nutrition", Activity = "NAV3", RegDate = DateTime.Now, CardPrinted = false },

        //        new User { IndividualId = "007", FullName = "David Dan Ochieng", Mission = "IOM South Sudan", HouseholdId = "SOBA0100144", FamilySize = 2, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Protection", Activity = "NAV1", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "008", FullName = "Grace Mary Akinyi", Mission = "IOM South Sudan", HouseholdId = "SOBA0100155", FamilySize = 8, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Health", Activity = "NAV2", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "009", FullName = "Samuel Kiptoo", Mission = "IOM South Sudan", HouseholdId = "SOBA0100166", FamilySize = 4, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Education", Activity = "NAV3", RegDate = DateTime.Now, CardPrinted = false },

        //        new User { IndividualId = "010", FullName = "Amina Hassan Yusuf", Mission = "IOM South Sudan", HouseholdId = "SOBA0100177", FamilySize = 6, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Shelter", Activity = "NAV1", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "011", FullName = "Michael Smith Otieno", Mission = "IOM South Sudan", HouseholdId = "SOBA0100188", FamilySize = 5, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "WASH", Activity = "NAV1", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "012", FullName = "Sarah Chebet", Mission = "IOM South Sudan", HouseholdId = "SOBA0100199", FamilySize = 3, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Nutrition", Activity = "NAV2", RegDate = DateTime.Now, CardPrinted = false },

        //        new User { IndividualId = "013", FullName = "Hassan Ali Omar", Mission = "IOM South Sudan", HouseholdId = "SOBA0100200", FamilySize = 7, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Protection", Activity = "NAV3", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "014", FullName = "Lucy Mary Njeri", Mission = "IOM South Sudan", HouseholdId = "SOBA0100211", FamilySize = 4, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Health", Activity = "NAV1", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "015", FullName = "Brian Kariuki", Mission = "IOM South Sudan", HouseholdId = "SOBA0100222", FamilySize = 6, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Education", Activity = "NAV1", RegDate = DateTime.Now, CardPrinted = false },

        //        new User { IndividualId = "016", FullName = "Esther Wairimu", Mission = "IOM South Sudan", HouseholdId = "SOBA0100233", FamilySize = 5, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Shelter", Activity = "NAV1", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "017", FullName = "Daniel Mutiso", Mission = "IOM South Sudan", HouseholdId = "SOBA0100244", FamilySize = 3, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "WASH", Activity = "NAV3", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "018", FullName = "Zainab Ali", Mission = "IOM South Sudan", HouseholdId = "SOBA0100255", FamilySize = 8, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Nutrition", Activity = "NAV3", RegDate = DateTime.Now, CardPrinted = false },

        //        new User { IndividualId = "019", FullName = "Paul Kamau", Mission = "IOM South Sudan", HouseholdId = "SOBA0100266", FamilySize = 4, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Protection", Activity = "NAV1", RegDate = DateTime.Now, CardPrinted = false },
        //        new User { IndividualId = "020", FullName = "Mercy Atieno", Mission = "IOM South Sudan", HouseholdId = "SOBA0100277", FamilySize = 6, LocationInformation = "Bentiu IDP Camp Sector 3", AdditionalInformation = "Z:3 B:1 P:0824", Program = "Health", Activity = "NAV2", RegDate = DateTime.Now, CardPrinted = false }

        //    };
        //}

        public List<User> GetByHousehold(string householdId)
        {
            return _users
                .Where(u => !u.CardPrinted &&
                            u.HouseholdId.Equals(householdId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<User> GetByActivity(string activity)
        {
            return _users
                .Where(u => !u.CardPrinted &&
                            u.Activity.Equals(activity, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<User> GetByHouseholdList(List<string> householdIds)
        {
            var set = householdIds
                .Select(x => x.ToLower())
                .ToHashSet();

            return _users
                .Where(u => !u.CardPrinted &&
                            set.Contains(u.HouseholdId.ToLower()))
                .ToList();
        }
    }
}
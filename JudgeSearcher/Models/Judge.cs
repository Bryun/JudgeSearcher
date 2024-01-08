using System;
using System.Collections.Generic;

namespace JudgeSearcher.Models
{
    public class Judge : Notification, ICloneable
    {
        #region Declarations

        string id, type, firstName, lastName, judicialAssistant, phone, location, street, city, zip, county, circuit, district, courtRoom, hearingRoom, subDivision;

        #endregion

        #region Properties

        public string ID
        {
            get => id;
            set
            {
                id = value;
                OnPropertyChanged();
            }
        }

        public string Type
        {
            get => type;
            set
            {
                type = value;
                OnPropertyChanged();
            }
        }

        public string FirstName
        {
            get => firstName;
            set
            {
                firstName = value;
                OnPropertyChanged();
            }
        }

        public string LastName
        {
            get => lastName;
            set
            {
                lastName = value;
                OnPropertyChanged();
            }
        }

        public string JudicialAssistant
        {
            get => judicialAssistant;
            set
            {
                judicialAssistant = value;
                OnPropertyChanged();
            }
        }

        public string Phone
        {
            get => phone;
            set
            {
                phone = value;
                OnPropertyChanged();
            }
        }

        public string Location
        {
            get => location;
            set
            {
                location = value;
                OnPropertyChanged();
            }
        }

        public string Street
        {
            get => street;
            set
            {
                street = value;
                OnPropertyChanged();
            }
        }

        public string City
        {
            get => city;
            set
            {
                city = value;
                OnPropertyChanged();
            }
        }

        public string Zip
        {
            get => zip;
            set
            {
                zip = value;
                OnPropertyChanged();
            }
        }

        public string County
        {
            get => county;
            set
            {
                county = value;
                OnPropertyChanged();
            }
        }

        public string Circuit
        {
            get => circuit;
            set
            {
                circuit = value;
                OnPropertyChanged();
            }
        }

        public string District
        {
            get => district;
            set
            {
                district = value;
                OnPropertyChanged();
            }
        }

        public string CourtRoom
        {
            get => courtRoom;
            set
            {
                courtRoom = value;
                OnPropertyChanged();
            }
        }

        public string HearingRoom
        {
            get => hearingRoom;
            set
            {
                hearingRoom = value;
                OnPropertyChanged();
            }
        }

        public string SubDivision
        {
            get => subDivision;
            set
            {
                subDivision = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Methods

        public Dictionary<string, string> Map
        {
            get => new Dictionary<string, string>
            {
                {"ID", ID},
                {"Type", Type},
                {"FirstName", FirstName},
                {"LastName", LastName},
                {"JudicialAssistant", JudicialAssistant},
                {"Phone", Phone},
                {"Location", Location},
                {"Street", Street},
                {"City", City},
                {"Zip", Zip},
                {"County", County},
                {"Circuit", Circuit},
                {"District", District},
                {"CourtRoom", CourtRoom},
                {"HearingRoom", HearingRoom},
                {"SubDivision", SubDivision}

            };
        }

        public object Clone()
        {
            return (Judge)MemberwiseClone();
        }

        #endregion
    }
}

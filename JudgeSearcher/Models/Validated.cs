using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using JudgeSearcher.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeSearcher.Models
{
    public class Validated : Judge
    {
        #region Constructor

        public Validated(string circuit, string county, string type, string firstName, string lastName) 
        { 
            Circuit = circuit;
            County = county;
            Type = type;
            FirstName = firstName;
            LastName = lastName;
        }

        #endregion

        #region Properties

        public bool _exists = false;
        public bool Exists
        {
            get => _exists;
            set
            {
                _exists = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}

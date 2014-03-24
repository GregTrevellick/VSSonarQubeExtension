﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LicenseOptionsModel.cs" company="Copyright © 2013 Tekla Corporation. Tekla is a Trimble Company">
//     Copyright (C) 2013 [Jorge Costa, Jorge.Costa@tekla.com]
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License
// as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details. 
// You should have received a copy of the GNU Lesser General Public License along with this program; if not, write to the Free
// Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
// --------------------------------------------------------------------------------------------------------------------

namespace VSSonarExtension.MainViewModel.ViewModel
{
    using System.Collections.ObjectModel;

    using ExtensionHelpers;

    using ExtensionTypes;

    /// <summary>
    ///     The plugins options model.
    /// </summary>
    public partial class ExtensionOptionsModel
    {
        /// <summary>
        /// The is license enable.
        /// </summary>
        private bool isLicenseEnable;

        /// <summary>
        /// The available licenses.
        /// </summary>
        private ObservableCollection<VsLicense> availableLicenses = new ObservableCollection<VsLicense>();

        /// <summary>
        /// The selected license.
        /// </summary>
        private VsLicense selectedLicense;

        /// <summary>
        /// The error message.
        /// </summary>
        private string errorMessage;

        /// <summary>
        /// The generated token.
        /// </summary>
        private string generatedToken;

        /// <summary>
        /// Gets or sets the available licenses.
        /// </summary>
        public ObservableCollection<VsLicense> AvailableLicenses
        {
            get
            {
                return this.availableLicenses;
            }

            set
            {
                this.availableLicenses = value;
                this.OnPropertyChanged("SelectedPluginItem");
                this.OnPropertyChanged("AvailableLicenses");
                this.selectedPluginItem = "License Manager";
            }
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                return this.errorMessage;
            }

            set
            {
                this.errorMessage = value;
                this.OnPropertyChanged("ErrorMessage");
            }
        }

        /// <summary>
        /// Gets or sets the selected license.
        /// </summary>
        public VsLicense SelectedLicense
        {
            get
            {
                return this.selectedLicense;
            }

            set
            {
                this.selectedLicense = value;
                if (value == null)
                {
                    this.errorMessage = string.Empty;
                    this.GeneratedToken = string.Empty;
                }
                else
                {
                    this.ErrorMessage = this.selectedLicense.ErrorMessage;
                }

                this.OnPropertyChanged("SelectedLicense");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether is license enable.
        /// </summary>
        public bool IsLicenseEnable
        {
            get
            {
                return this.isLicenseEnable;
            }

            set
            {
                this.isLicenseEnable = value;
                this.OnPropertyChanged("IsLicenseEnable");
            }
        }

        /// <summary>
        /// Gets or sets the generated token.
        /// </summary>
        public string GeneratedToken
        {
            get
            {
                return this.generatedToken;
            }

            set
            {
                this.generatedToken = value;
                this.OnPropertyChanged("GeneratedToken");
            }
        }

        /// <summary>
        /// The refresh licenses.
        /// </summary>
        /// <returns>
        /// The <see>
        ///         <cref>ObservableCollection</cref>
        ///     </see>
        ///     .
        /// </returns>
        public ObservableCollection<VsLicense> GetLicensesFromServer()
        {
            var licenses = new ObservableCollection<VsLicense>();

            if (this.plugins != null)
            {
                foreach (var plugin in this.plugins)
                {
                    var lics = plugin.GetLicenses(ConnectionConfigurationHelpers.GetConnectionConfiguration(this.Vsenvironmenthelper));
                    if (lics != null)
                    {
                        foreach (var license in lics)
                        {
                            bool existsAlready = false;
                            foreach (var existinglicense in licenses)
                            {
                                if (existinglicense.LicenseTxt.Equals(license.Value.LicenseTxt))
                                {
                                    existsAlready = true;
                                }
                            }

                            if (!existsAlready)
                            {
                                licenses.Add(license.Value);
                            }                            
                        }
                    }
                }
            }

            return licenses;
        }
    }
}

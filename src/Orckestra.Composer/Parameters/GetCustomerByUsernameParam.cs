﻿using System;
using System.Globalization;

namespace Orckestra.Composer.Parameters
{
    /// <summary>
    /// Repository call param to retreive a single Customer based on it's unique identifier
    /// </summary>
    public class GetCustomerByUsernameParam
    {
        /// <summary>
        /// (Mandatory)
        /// The username of the customer to look for
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// (Mandatory)
        /// The scope to which the Customer must belong
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// (Mandatory)
        /// The Culture for any displayable values
        /// </summary>
        public CultureInfo CultureInfo { get; set; }
    }
}

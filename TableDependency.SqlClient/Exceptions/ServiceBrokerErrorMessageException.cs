﻿////////////////////////////////////////////////////////////////////////////////
//   TableDependency, SqlTableDependency, OracleTableDependency
//   Copyright (c) Christian Del Bianco.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////
using System;
using TableDependency.Exceptions;

namespace TableDependency.SqlClient.Exceptions
{
    [Serializable]
    public class ServiceBrokerErrorMessageException : TableDependencyException
    {
        protected internal ServiceBrokerErrorMessageException(string naming)
            : base($"Service broker {naming} send an error message.")
        { }
    }
}
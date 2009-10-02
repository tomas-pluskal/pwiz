﻿/*
 * Original author: Nick Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using NHibernate;
using NHibernate.Metadata;

namespace pwiz.Skyline.Model.Hibernate.Query
{
    public class Schema
    {
        private readonly ISessionFactory _sessionFactory;
        public Schema(ISessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        public IClassMetadata GetClassMetadata(Type type)
        {
            return _sessionFactory.GetClassMetadata(type);
        }
        public ColumnInfo GetColumnInfo(Type table, String column)
        {
            PropertyInfo propertyInfo = table.GetProperty(column);
            ColumnInfo columnInfo = new ColumnInfo
            {
                Identifier = new Identifier(column),
                Caption = column
            };
            foreach (QueryColumn attr in propertyInfo.GetCustomAttributes(typeof(QueryColumn), true))
            {
                columnInfo.Caption = attr.FullName ?? columnInfo.Caption;
                columnInfo.Format = attr.Format ?? columnInfo.Format;
                columnInfo.ColumnType = propertyInfo.PropertyType;
            }
            return columnInfo;
        }
        public ColumnInfo GetColumnInfo(Type table, Identifier column)
        {
            Type lastTable;
            String columnName;
            Resolve(table, column, out lastTable, out columnName);
            ColumnInfo result = GetColumnInfo(lastTable, columnName);
            result.Identifier = column;
            return result;
        }
        public IList<Type> GetTables()
        {
            List<Type> result = new List<Type>();
            foreach (Type table in _sessionFactory.GetAllClassMetadata().Keys)
            {
                result.Add(table);
            }
            return result;
        }
        public bool Resolve(Type table, Identifier identifier, out Type resultTable, out String column)
        {
            if (identifier.Parts.Count == 1)
            {
                resultTable = table;
                column = identifier.Parts[0];
                return true;
            }
            PropertyInfo propertyInfo = table.GetProperty(identifier.Parts[0]);
            return Resolve(propertyInfo.PropertyType, identifier.RemovePrefix(1), out resultTable, out column);
        }
    }
}

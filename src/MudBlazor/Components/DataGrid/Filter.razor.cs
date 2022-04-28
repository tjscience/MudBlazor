// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace MudBlazor
{
    public partial class Filter<T>
    {
        [CascadingParameter] public MudDataGrid<T> DataGrid { get; set; }

        [Parameter] public Guid Id { get; set; }
        [Parameter] public string Field { get; set; }
        [Parameter] public Type FieldType { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public string Operator { get; set; }
        
        [Parameter] public object Value
        {
            get => _value;
            set
            {
                if(_value != value)
                {
                    _value = value;
                    OnInitialized();
                }            
            }
        }

        [Parameter] public EventCallback<string> FieldChanged { get; set; }
        [Parameter] public EventCallback<string> TitleChanged { get; set; }
        [Parameter] public EventCallback<string> OperatorChanged { get; set; }
        [Parameter] public EventCallback<object> ValueChanged { get; set; }

        private object _value;
        private string _valueString;
        private double? _valueNumber;
        private Enum _valueEnum = null;
        private bool? _valueBool;
        private DateTime? _valueDate;
        private TimeSpan? _valueTime;

        #region Computed Properties and Functions

        private Type dataType
        {
            get
            {
                if (FieldType != null)
                    return FieldType;

                if (Field == null)
                    return typeof(object);

                if (typeof(T) == typeof(IDictionary<string, object>) && FieldType == null)
                    throw new ArgumentNullException(nameof(FieldType));

                var t = typeof(T).GetProperty(Field).PropertyType;
                return Nullable.GetUnderlyingType(t) ?? t;
            }
        }

        private bool isNumber
        {
            get
            {
                return FilterOperator.IsNumber(dataType);
            }
        }
        private bool isEnum
        {
            get
            {
                return FilterOperator.IsEnum(dataType);
            }
        }

        #endregion


        protected override void OnInitialized()
        {
            if (dataType == typeof(string))
                _valueString = Value == null ? null : Value.ToString();
            else if (isNumber)
                _valueNumber = Value == null ? null : Convert.ToDouble(Value);
            else if (isEnum)
                _valueEnum = Value == null ? null : (Enum)Value;
            else if (dataType == typeof(bool))
                _valueBool = Value == null ? null : Convert.ToBoolean(Value);
            else if (dataType == typeof(DateTime) || dataType == typeof(DateTime?))
            {
                var dateTime = Convert.ToDateTime(Value);
                _valueDate = Value == null ? null : dateTime;
                _valueTime = Value == null ? null : dateTime.TimeOfDay;
            }
        }

        internal async Task FieldChangedAsync(string field)
        {
            
            Value = null;           
            await ValueChanged.InvokeAsync(Value);
            OnInitialized();

            Field = field;
            await FieldChanged.InvokeAsync(Field);

            var operators = FilterOperator.GetOperatorByDataType(dataType);
            Operator = operators.FirstOrDefault();

            await OperatorChanged.InvokeAsync(Operator);
        }

        internal void TitleChangedAsync(string field)
        {
            Field = field;
        }

        internal async Task StringValueChangedAsync(string value)
        {
            Value = value;
            _valueString = value;
            await ValueChanged.InvokeAsync(value);
            DataGrid.GroupItems();
        }

        internal async Task NumberValueChangedAsync(double? value)
        {
            Value = value;
            _valueNumber = value;
            await ValueChanged.InvokeAsync(value);
            DataGrid.GroupItems();
        }

        internal async Task EnumValueChangedAsync(Enum value)
        {
            Value = value;
            _valueEnum = value;
            await ValueChanged.InvokeAsync(value);
            DataGrid.GroupItems();
        }

        internal async Task BoolValueChangedAsync(bool? value)
        {
            Value = value;
            _valueBool = value;
            await ValueChanged.InvokeAsync(value);
            DataGrid.GroupItems();
        }

        internal async Task DateValueChangedAsync(DateTime? value)
        {
            _valueDate = value;

            if (value != null)
            {
                var date = value.Value.Date;

                // get the time component and add it to the date.
                if (_valueTime != null)
                {
                    date = date.Add(_valueTime.Value);
                }
                Value = date;
                await ValueChanged.InvokeAsync(date);
            }
            else
            {
                Value = value;
                await ValueChanged.InvokeAsync(value);
            }

            DataGrid.GroupItems();
        }

        internal async Task TimeValueChangedAsync(TimeSpan? value)
        {
            _valueTime = value;

            if (_valueDate != null)
            {
                var date = _valueDate.Value.Date;

                
                // get the time component and add it to the date.
                if (_valueTime != null)
                {
                    date = date.Add(_valueTime.Value);
                }

                Value = date;
                await ValueChanged.InvokeAsync(date);
            }

            DataGrid.GroupItems();
        }

        internal async Task OperatorChangedAsync(string op)
        {
            Operator = op;
            await OperatorChanged.InvokeAsync(Operator);
        }

        internal void RemoveFilter()
        {
            DataGrid.RemoveFilter(Id);
        }
    }
}

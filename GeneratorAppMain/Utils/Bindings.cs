﻿using System;
using System.Windows.Forms;

namespace GeneratorAppMain.Utils
{
    public static class Bindings
    {
        public static Binding VisibleNullableBinding(object dataSource, string property)
        {
            var binding = new Binding("Visible", dataSource, property, true);
            binding.Format += NullableToBooleanFormat;
            return binding;
        }

        public static Binding NegativeVisibleBinding(object dataSource, string property)
        {
            var binding = new Binding("Visible", dataSource, property, true);
            binding.Format += NegativeBooleanFormat;
            return binding;
        }

        public static Binding NegativeBinding(string propertyName, object dataSource, string dataMember)
        {
            var binding = new Binding(propertyName, dataSource, dataMember, true);
            binding.Format += NegativeBooleanFormat;
            return binding;
        }

        private static void NegativeBooleanFormat(object sender, ConvertEventArgs e)
        {
            e.Value = !(bool)e.Value;
        }

        private static void NullableToBooleanFormat(object sender, ConvertEventArgs e)
        {
            e.Value = e.Value != null;
        }
    }
}
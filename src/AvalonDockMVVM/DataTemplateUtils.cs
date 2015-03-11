using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace AvalonDockMVVM
{
    /// <summary>
    /// Helper class for lookup of data-templates.
    /// </summary>
    internal static class DataTemplateUtils
    {
        /// <summary>
        /// Find a DataTemplate for the specified type in the visual-tree.
        /// </summary>
        public static DataTemplate FindDataTemplate(Type type, FrameworkElement element)
        {
            var dataTemplate = element.TryFindResource(new DataTemplateKey(type)) as DataTemplate;
            if (dataTemplate != null)
            {
                return dataTemplate;
            }

            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                dataTemplate = FindDataTemplate(type.BaseType, element);
                if (dataTemplate != null)
                {
                    return dataTemplate;
                }
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                dataTemplate = FindDataTemplate(interfaceType, element);
                if (dataTemplate != null)
                {
                    return dataTemplate;
                }
            }

            return null;
        }

        /// <summary>
        /// Find a data-template for the specified type and instance a visual from it.
        /// </summary>
        public static FrameworkElement InstanceTemplate(Type type, FrameworkElement element, object dataContext)
        {
            var dataTemplate = FindDataTemplate(type, element);
            if (dataTemplate == null)
            {
                return null;
            }

            return InstanceTemplate(dataTemplate, dataContext);
        }

        /// <summary>
        /// Instance a visual element from a data template.
        /// </summary>
        public static FrameworkElement InstanceTemplate(DataTemplate dataTemplate, object dataContext)
        {
            var element = (FrameworkElement)dataTemplate.LoadContent();
            element.DataContext = dataContext;
            return element;
        }
    }
}

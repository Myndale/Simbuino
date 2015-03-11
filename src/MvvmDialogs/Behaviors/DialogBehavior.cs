using MvvmDialogs.Presenters;
using MvvmDialogs.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MvvmDialogs.Behaviors
{
	public static class DialogBehavior
	{
		private static Dictionary<IDialogViewModel, Window> DialogBoxes = new Dictionary<IDialogViewModel, Window>();
		private static Dictionary<Window, NotifyCollectionChangedEventHandler> ChangeNotificationHandlers = new Dictionary<Window, NotifyCollectionChangedEventHandler>();

		public static readonly DependencyProperty DialogViewModelsProperty = DependencyProperty.RegisterAttached(
			"DialogViewModels",
			typeof(object),
			typeof(DialogBehavior),
			new PropertyMetadata(null, OnDialogViewModelsChange));

		public static void SetDialogViewModels(DependencyObject source, object value)
		{
			source.SetValue(DialogViewModelsProperty, value);
		}

		public static object GetDialogViewModels(DependencyObject source)
		{
			return source.GetValue(DialogViewModelsProperty);
		}

		public static readonly DependencyProperty ClosingProperty = DependencyProperty.RegisterAttached(
			"Closing",
			typeof(bool),
			typeof(DialogBehavior),
			new PropertyMetadata(false));

		public static readonly DependencyProperty ClosedProperty = DependencyProperty.RegisterAttached(
			"Closed",
			typeof(bool),
			typeof(DialogBehavior),
			new PropertyMetadata(false));

		private static void OnDialogViewModelsChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var parent = d as Window;
			if (parent == null)
				return;

			// when the parent closes we don't need to track it anymore
			parent.Closed += (s, a) => ChangeNotificationHandlers.Remove(parent);

			// otherwise create a handler for it that responds to changes to the supplied collection
			if (!ChangeNotificationHandlers.ContainsKey(parent))
				ChangeNotificationHandlers[parent] = (sender, args) =>
				{
					var collection = sender as ObservableCollection<IDialogViewModel>;
					if (collection != null)
					{
						if (args.NewItems != null)
							foreach (IDialogViewModel viewModel in args.NewItems)
								AddDialog(viewModel, collection, d as Window);

						if (args.OldItems != null)
							foreach (IDialogViewModel viewModel in args.OldItems)
								RemoveDialog(viewModel);
					}
				};

			// when the collection is first bound to this property we should create any initial
			// dialogs the user may have added in the main view model's constructor
			var newCollection = e.NewValue as ObservableCollection<IDialogViewModel>;
			if (newCollection != null)
			{
				newCollection.CollectionChanged += ChangeNotificationHandlers[parent];
				foreach (IDialogViewModel viewModel in newCollection.ToList())
					AddDialog(viewModel, newCollection, d as Window);
			}

			// when we remove the binding we need to shut down any dialogs that have been left open
			var oldCollection = e.OldValue as ObservableCollection<IDialogViewModel>;
			if (oldCollection != null)
			{
				oldCollection.CollectionChanged -= ChangeNotificationHandlers[parent];
				foreach (IDialogViewModel viewModel in oldCollection.ToList())
					RemoveDialog(viewModel);
			}
		}

		private static void AddDialog(IDialogViewModel viewModel, ObservableCollection<IDialogViewModel> collection, Window owner)
		{
			// find the global resource that has been keyed to this view model type
			var resource = Application.Current.TryFindResource(viewModel.GetType());
			if (resource == null)
				return;

			// is this resource a presenter?
			if (IsAssignableToGenericType(resource.GetType(), typeof(IDialogBoxPresenter<>)))
			{
				resource.GetType().GetMethod("Show").Invoke(resource, new object[] { viewModel });
				collection.Remove(viewModel);
			}

			// is this resource a dialog box window?
			else if (resource is Window)
			{
				var userViewModel = viewModel as IUserDialogViewModel;
				if (userViewModel == null)
					return;
				var dialog = resource as Window;
				dialog.DataContext = userViewModel;
				DialogBoxes[userViewModel] = dialog;
				userViewModel.DialogClosing += (sender, args) =>
					collection.Remove(sender as IUserDialogViewModel);
				dialog.Closing += (sender, args) =>
				{
					if (!(bool)dialog.GetValue(ClosingProperty))
					{
						dialog.SetValue(ClosingProperty, true);
						userViewModel.RequestClose();
						if (!(bool)dialog.GetValue(ClosedProperty))
						{
							args.Cancel = true;
							dialog.SetValue(ClosingProperty, false);
						}
					}
				};
				dialog.Closed += (sender, args) =>
				{
					Debug.Assert(DialogBoxes.ContainsKey(userViewModel));
					DialogBoxes.Remove(userViewModel);
					return;
				};
				dialog.Owner = owner;
				if (userViewModel.IsModal)
					dialog.ShowDialog();
				else
					dialog.Show();
			}
		}

		private static void RemoveDialog(IDialogViewModel viewModel)
		{
			if (DialogBoxes.ContainsKey(viewModel))
			{
				var dialog = DialogBoxes[viewModel];
				if (!(bool)dialog.GetValue(ClosingProperty))
				{
					dialog.SetValue(ClosingProperty, true);
					DialogBoxes[viewModel].Close();
				}
				dialog.SetValue(ClosedProperty, true);
			}
		}
		

		// courtesy James Fraumeni/StackOverflow: http://stackoverflow.com/questions/74616/how-to-detect-if-type-is-another-generic-type/1075059#1075059
		private static bool IsAssignableToGenericType(Type givenType, Type genericType)
		{
			var interfaceTypes = givenType.GetInterfaces();

			foreach (var it in interfaceTypes)
			{
				if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
					return true;
			}

			if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
				return true;

			Type baseType = givenType.BaseType;
			if (baseType == null) return false;

			return IsAssignableToGenericType(baseType, genericType);
		}
	}

}

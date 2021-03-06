﻿using System.Linq;
using System.Windows;
using System.Windows.Input;
using IrcSays.Application;
using IrcSays.Communication.Irc;

namespace IrcSays.Ui
{
	public partial class ChatWindow : Window
	{
		public static readonly RoutedUICommand ChatCommand = new RoutedUICommand("Chat", "Chat", typeof (ChatWindow));
		public static readonly RoutedUICommand CloseTabCommand = new RoutedUICommand("Close", "CloseTab", typeof (ChatWindow));

		public static readonly RoutedUICommand NewTabCommand = new RoutedUICommand("New Server Tab", "NewTab",
			typeof (ChatWindow));

		public static readonly RoutedUICommand DetachCommand = new RoutedUICommand("Detach", "Detach", typeof (ChatWindow));

		public static readonly RoutedUICommand PreviousTabCommand = new RoutedUICommand("Previous Tab", "PreviousTab",
			typeof (ChatWindow));

		public static readonly RoutedUICommand NextTabCommand = new RoutedUICommand("Next Tab", "NextTab", typeof (ChatWindow));

		public static readonly RoutedUICommand SettingsCommand = new RoutedUICommand("Settings", "Settings",
			typeof (ChatWindow));

		public static readonly RoutedUICommand MinimizeCommand = new RoutedUICommand("Minimize", "Minimize",
			typeof (ChatWindow));

		public static readonly RoutedUICommand MaximizeCommand = new RoutedUICommand("Maximize", "Maximize",
			typeof (ChatWindow));

		public static readonly RoutedUICommand CloseCommand = new RoutedUICommand("Quit", "Close", typeof (ChatWindow));

		private void ExecuteChat(object sender, ExecutedRoutedEventArgs e)
		{
			var control = tabsChat.SelectedContent as ChatPage;
			App.Create(control.Session, new IrcTarget((string) e.Parameter), true);
		}

		private void ExecuteCloseTab(object sender, ExecutedRoutedEventArgs e)
		{
			var page = e.Parameter as ChatPage;
			if (page != null)
			{
				if (page.Type == ChatPageType.Server)
				{
					if (page.Session.State == IrcSessionState.Disconnected ||
						App.Settings.Current.Windows.SuppressWarningOnQuit ||
						ConfirmQuit(string.Format("Are you sure you want to disconnect from {0}?", page.Session.NetworkName),
							"Close Server Tab"))
					{
						if (page.Session.State != IrcSessionState.Disconnected)
						{
							page.Session.Quit("Leaving");
						}
						var itemsToRemove = (from i in Items
							where i.Page.Session == page.Session && (i.Page.Type == ChatPageType.Chat || i.Page.Type == ChatPageType.Server)
							select i.Page).ToArray();
						foreach (var p in itemsToRemove)
						{
							RemovePage(p);
						}
					}
				}
				else if (page.Type == ChatPageType.Chat)
				{
					if (page.Target.IsChannel &&
						page.Session.State != IrcSessionState.Disconnected)
					{
						page.Session.Part(page.Target.Name);
					}
					RemovePage(page);
				}
				else
				{
					_isInModalDialog = true;
					if (page.CanClose())
					{
						RemovePage(page);
					}
					_isInModalDialog = false;
				}
			}
		}

		private void ExecuteNewTab(object sender, ExecutedRoutedEventArgs e)
		{
			AddPage(new ChatControl(ChatPageType.Server, new IrcSession(), null), true);
		}

		private void ExecuteDetach(object sender, ExecutedRoutedEventArgs e)
		{
			var item = e.Parameter as ChatTabItem;
			if (item != null &&
				item.Page.Type != ChatPageType.Server)
			{
				Items.Remove(item);
				var ctrl = item.Content;
				item.Content = null;
				var window = new ChannelWindow(item.Page);
				window.Show();
			}
		}

		private void CanExecuteCloseTab(object sender, CanExecuteRoutedEventArgs e)
		{
			var page = e.Parameter as ChatPage;
			if (page != null)
			{
				if (page.Type == ChatPageType.Server)
				{
					e.CanExecute = Items.Count((i) => i.Page.Type == ChatPageType.Server) > 1 && page.IsCloseable;
				}
				else
				{
					e.CanExecute = page.IsCloseable;
				}
			}
		}

		private void ExecutePreviousTab(object sender, ExecutedRoutedEventArgs e)
		{
			tabsChat.SelectedIndex--;
		}

		private void ExecuteNextTab(object sender, ExecutedRoutedEventArgs e)
		{
			tabsChat.SelectedIndex++;
		}

		private void CanExecutePreviousTab(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = tabsChat.SelectedIndex > 0;
		}

		private void CanExecuteNextTab(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = tabsChat.SelectedIndex < tabsChat.Items.Count - 1;
		}

		private void ExecuteSettings(object sender, ExecutedRoutedEventArgs e)
		{
			App.ShowSettings();
		}

		private void ExecuteMinimize(object sender, ExecutedRoutedEventArgs e)
		{
			_oldWindowState = WindowState;
			WindowState = WindowState.Minimized;
		}

		private void ExecuteMaximize(object sender, ExecutedRoutedEventArgs e)
		{
			if (WindowState == WindowState.Maximized)
			{
				WindowState = WindowState.Normal;
			}
			else
			{
				WindowState = WindowState.Maximized;
			}
		}

		private void ExecuteClose(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}
	}
}
using Reactive.Bindings.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRTXamlToolkit.Async;

namespace Uncord.Views.Behaviors
{
    public class ScrollViewerAutoScrollToLatestItemBehavior : Behavior<ScrollViewer>
    {

        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register(
                nameof(IsEnabled),
                typeof(bool),
                typeof(ScrollViewerAutoScrollToLatestItemBehavior),
                new PropertyMetadata((true))
                );


        public bool IsAlwaysAutoScrollOnAdded
        {
            get { return (bool)GetValue(IsAlwaysAutoScrollOnAddedProperty); }
            set { SetValue(IsAlwaysAutoScrollOnAddedProperty, value); }
        }

        public static readonly DependencyProperty IsAlwaysAutoScrollOnAddedProperty =
            DependencyProperty.Register(
                nameof(IsAlwaysAutoScrollOnAdded),
                typeof(bool),
                typeof(ScrollViewerAutoScrollToLatestItemBehavior),
                new PropertyMetadata((false))
                );


        public ItemsControl ObserveCollection
        {
            get { return (ItemsControl)GetValue(ObserveCollectionProperty); }
            set { SetValue(ObserveCollectionProperty, value); }
        }

        public static readonly DependencyProperty ObserveCollectionProperty =
            DependencyProperty.Register(
                nameof(ObserveCollection),
                typeof(ItemsControl),
                typeof(ScrollViewerAutoScrollToLatestItemBehavior),
                new PropertyMetadata(default(ItemsControl))
                );



        BehaviorSubject<long> AutoScrollSubeject = new BehaviorSubject<long>(0);
        AsyncLock AutoScrollLock = new AsyncLock();
        IDisposable _AutoScrollSubscriber;
        bool _FirstScroll = true;

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            base.OnAttached();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            _AutoScrollSubscriber = AutoScrollSubeject
                .Throttle(TimeSpan.FromSeconds(0.25))
                .Subscribe(async _ =>
                {
                    using (var releaser = await AutoScrollLock.LockAsync())
                    {
                        Debug.WriteLine("1");
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            Debug.WriteLine("2");
                            ScrollToLatest();
                            Debug.WriteLine("3");
                        });
                        Debug.WriteLine("4");
                    }

                }, (_) => { Debug.WriteLine("error"); }, () => { Debug.WriteLine("complete"); });

            var child = AssociatedObject.Content as FrameworkElement;
            if (child == null) { return; }
            child.SizeChanged += Child_SizeChanged;

            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            var child = AssociatedObject.Content as FrameworkElement;
            if (child == null) { return; }
            child.SizeChanged -= Child_SizeChanged;

            _AutoScrollSubscriber?.Dispose();
            _AutoScrollSubscriber = null;

        }

        private void Child_SizeChanged(object sender, object e)
        {
            if (ObserveCollection != null && ObserveCollection.Items.Count > 1)
            {
                var latestItem = ObserveCollection.Items[ObserveCollection.Items.Count - 1];
                var prevLatestItem = ObserveCollection.Items[ObserveCollection.Items.Count - 2];
                var latestContainer = ObserveCollection.ContainerFromItem(latestItem) as FrameworkElement;
                var prevLatestContainer = ObserveCollection.ContainerFromItem(prevLatestItem) as FrameworkElement;

                var height = latestContainer.ActualHeight + prevLatestContainer.ActualHeight;
                var scrollViewer = AssociatedObject;

                // 追加される前の最新アイテムが画面下端にある場合
                if (scrollViewer.ScrollableHeight - scrollViewer.VerticalOffset < height)
                {
                    AutoScrollSubeject.OnNext(0);
                }
            }
            else
            {
                AutoScrollSubeject.OnNext(0);
            }
        }


        private void ScrollToLatest()
        {
            if (!IsEnabled) { return; }
            var isReadLatest = true; // 最新のアイテムが画面に表示されている場合 true

            if (_FirstScroll || IsAlwaysAutoScrollOnAdded || isReadLatest)
            {
                var scrollViewer = AssociatedObject as ScrollViewer;
                scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                Debug.WriteLine(scrollViewer.ScrollableHeight);

                if (scrollViewer.ScrollableHeight > 0)
                {
                    _FirstScroll = false;
                }
            }
        }

    }
}

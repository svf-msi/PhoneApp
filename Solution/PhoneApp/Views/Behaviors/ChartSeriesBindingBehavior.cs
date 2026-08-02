using MicroVue.Models;
using Syncfusion.Maui.Charts;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroVue.Views
{
    public class ChartSeriesBindingBehavior : Behavior<SfCartesianChart>
    {
        public static readonly BindableProperty SeriesSourceProperty =
            BindableProperty.Create(nameof(SeriesSource), typeof(IEnumerable<LineSeriesModel>), typeof(ChartSeriesBindingBehavior), null, propertyChanged: OnSeriesSourceChanged);

        public IEnumerable<LineSeriesModel> SeriesSource
        {
            get => (IEnumerable<LineSeriesModel>)GetValue(SeriesSourceProperty);
            set => SetValue(SeriesSourceProperty, value);
        }

        private SfCartesianChart AssociatedChart { get; set; }

        protected override void OnAttachedTo(SfCartesianChart bindable)
        {
            base.OnAttachedTo(bindable);
            AssociatedChart = bindable;
        }

        protected override void OnDetachingFrom(SfCartesianChart bindable)
        {
            base.OnDetachingFrom(bindable);
            if (SeriesSource is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= OnCollectionChanged;
            AssociatedChart = null;
        }

        private static void OnSeriesSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var behavior = (ChartSeriesBindingBehavior)bindable;
            if (oldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= behavior.OnCollectionChanged;

            if (newValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += behavior.OnCollectionChanged;

            behavior.UpdateSeries();
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => UpdateSeries();

        private void UpdateSeries()
        {
            if (AssociatedChart == null || SeriesSource == null) return;

            AssociatedChart.Series.Clear();

            foreach (var seriesData in SeriesSource)
            {
                var lineSeries = new LineSeries
                {
                    ItemsSource = seriesData.Points,
                    XBindingPath = nameof(ChartDataPoint.X),
                    YBindingPath = nameof(ChartDataPoint.Y),
                    Label = seriesData.Name
                };
                AssociatedChart.Series.Add(lineSeries);
            }
        }
    }
}

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using KapeViewer.Models;

namespace KapeViewer.Services
{
    public class VirtualizingTimelineCollection : IList<TimelineEvent>, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly FilterCriteria _filterCriteria;
        private readonly int _pageSize = 1000;
        private readonly Dictionary<int, List<TimelineEvent>> _pages = new();
        private readonly Dictionary<int, DateTime> _pageAccessTimes = new();
        private int _count = -1;

        private bool _isUtcMode = true;

        public VirtualizingTimelineCollection(DatabaseService databaseService, FilterCriteria filterCriteria)
        {
            _databaseService = databaseService;
            _filterCriteria = filterCriteria;
        }

        public bool IsUtcMode
        {
            get => _isUtcMode;
            set
            {
                if (_isUtcMode != value)
                {
                    _isUtcMode = value;
                    Refresh();
                }
            }
        }

        public int Count
        {
            get
            {
                if (_count == -1)
                {
                    _count = _databaseService.GetEventCount(_filterCriteria);
                }
                return _count;
            }
        }

        public TimelineEvent this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                int pageIndex = index / _pageSize;
                int pageOffset = index % _pageSize;

                if (!_pages.ContainsKey(pageIndex))
                {
                    LoadPage(pageIndex);
                }

                // Update access time for LRU cache cleanup
                _pageAccessTimes[pageIndex] = DateTime.UtcNow;
                CleanupCache();

                return _pages[pageIndex][pageOffset];
            }
            set => throw new NotSupportedException();
        }

        private void LoadPage(int pageIndex)
        {
            int offset = pageIndex * _pageSize;
            var events = _databaseService.GetEvents(_filterCriteria, offset, _pageSize);
            
            // Adjust timestamps based on IsUtcMode
            foreach (var evt in events)
            {
                // Assume DB time is UTC
                var utcTime = DateTime.SpecifyKind(evt.Timestamp, DateTimeKind.Utc);
                evt.Timestamp = _isUtcMode ? utcTime : utcTime.ToLocalTime();
            }

            _pages[pageIndex] = events;
        }

        private void CleanupCache()
        {
            // Keep max 10 pages in memory
            if (_pages.Count > 10)
            {
                var oldestPage = _pageAccessTimes.OrderBy(x => x.Value).First().Key;
                _pages.Remove(oldestPage);
                _pageAccessTimes.Remove(oldestPage);
            }
        }

        public void Refresh()
        {
            _count = -1;
            _pages.Clear();
            _pageAccessTimes.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(nameof(Count));
        }

        #region Interface Implementations

        public bool IsReadOnly => true;
        public bool IsFixedSize => false;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public IEnumerator<TimelineEvent> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Add(object? value) => throw new NotSupportedException();
        public void Add(TimelineEvent item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(object? value) => throw new NotSupportedException();
        public bool Contains(TimelineEvent item) => throw new NotSupportedException();
        public int IndexOf(object? value) => throw new NotSupportedException();
        public int IndexOf(TimelineEvent item) => throw new NotSupportedException();
        public void Insert(int index, object? value) => throw new NotSupportedException();
        public void Insert(int index, TimelineEvent item) => throw new NotSupportedException();
        public void Remove(object? value) => throw new NotSupportedException();
        public bool Remove(TimelineEvent item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
        public void CopyTo(Array array, int index) => throw new NotSupportedException();
        public void CopyTo(TimelineEvent[] array, int arrayIndex) => throw new NotSupportedException();

        #endregion
    }
}

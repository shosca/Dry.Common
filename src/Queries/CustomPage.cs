#region using

using System;
using System.Collections;
using System.Collections.Generic;
using Castle.Components.Pagination;

#endregion

namespace Dry.Common.Queries {
    public class CustomPage<T> : IPaginatedPage<T> {
        readonly IEnumerable<T> _sourcelist;
        int _firstItemIndex, _lastItemIndex, _pageSize, _currentPageSize, _startIndex, _endIndex;
        int _previousPageIndex, _nextPageIndex, _lastPageIndex;
        readonly int _currentPageIndex;
        bool _hasPreviousPage, _hasNextPage;
        readonly int _totalitems;
        bool _calculated = false;

        protected void CalculatePaginationInfo() {
            if (_calculated) return;

            _startIndex = (_pageSize * _currentPageIndex);
            _endIndex = Math.Min(_startIndex + _pageSize, Convert.ToInt32(_totalitems));
            _firstItemIndex = _totalitems != 0 ? _startIndex + 1 : 0;
            _lastItemIndex = _endIndex;
            _previousPageIndex = _currentPageIndex - 1;
            _nextPageIndex = _currentPageIndex + 1;
            _lastPageIndex = _totalitems == -1 || _pageSize == 0 ? -1 : _totalitems / _pageSize;
            _hasPreviousPage = _currentPageIndex > 1;
            _currentPageSize = _totalitems > 0 ? _endIndex - _startIndex : 0;

            if (_totalitems != -1 && _totalitems / (float)_pageSize > _lastPageIndex)
            {
                _lastPageIndex++;
            }

            _hasNextPage = _totalitems == -1 || _currentPageIndex < _lastPageIndex;
            _calculated = true;
        }

        public CustomPage(IEnumerable<T> list, int currentpage, int pagesize, int total) {
            _sourcelist = list;
            _currentPageIndex = currentpage;
            _pageSize = pagesize;
            _totalitems = total;
        }

        public int CurrentPageIndex {
            get {
                CalculatePaginationInfo();
                return _currentPageIndex;
            }
        }

        public int PreviousPageIndex {
            get {
                CalculatePaginationInfo();
                return _previousPageIndex;
            }
        }

        public int NextPageIndex {
            get {
                CalculatePaginationInfo();
                return _nextPageIndex;
            }
        }

        public int FirstItemIndex {
            get {
                CalculatePaginationInfo();
                return _firstItemIndex;
            }
        }

        public int LastItemIndex {
            get {
                CalculatePaginationInfo();
                return _lastItemIndex;
            }
        }


        public int TotalItems {
            get {
                CalculatePaginationInfo();
                return _totalitems;
            }
        }

        public int PageSize {
            get {
                CalculatePaginationInfo();
                return _pageSize;
            }
        }

        public bool HasPreviousPage {
            get {
                CalculatePaginationInfo();
                return _hasPreviousPage;
            }
        }

        public bool HasNextPage {
            get {
                CalculatePaginationInfo();
                return _hasNextPage;
            }
        }

        public bool HasPage(int pageNumber) {
            CalculatePaginationInfo();
            return pageNumber <= _lastPageIndex && pageNumber >= 1;
        }

        public int TotalPages {
            get { CalculatePaginationInfo();
                return _lastPageIndex;
            }
        }

        public int CurrentPageSize {
            get { CalculatePaginationInfo();
                return _currentPageSize;
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return _sourcelist.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public T FirstItem {
            get {
                return PaginationSupport.GetItemAtIndex(_sourcelist, _firstItemIndex - ((this as IPaginatedPage).FirstItemIndex - 1));
            }
        }

        public T LastItem {
            get {
                return PaginationSupport.GetItemAtIndex(_sourcelist, _lastItemIndex - ((this as IPaginatedPage).FirstItemIndex - 1));
            }
        }

        object IPaginatedPage.FirstItem {
            get { return FirstItem; }
        }

        object IPaginatedPage.LastItem {
            get { return LastItem; }
        }
    }
}

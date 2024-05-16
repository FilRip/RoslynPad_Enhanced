using System.Globalization;
using System.Windows.Data;

using RoslynPad.UI;

namespace RoslynPad.Formatting;

public class DocumentCollectionViewConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        DocumentViewModel vm = (DocumentViewModel)value;
        ListCollectionView childrenView = new(vm.Children);
        childrenView.LiveFilteringProperties.Add(nameof(DocumentViewModel.IsSearchMatch));
        childrenView.IsLiveFiltering = true;
        childrenView.Filter = o => ((DocumentViewModel)o).IsSearchMatch;
        return childrenView;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

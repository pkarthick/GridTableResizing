using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TableResizing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            new TableResizingHelper(this.RTBCanvas, this.RTB);
           
        }


    }

    public class TableResizingHelper {

        private Canvas RTBCanvas;
        private RichTextBox RTB;

        public TableResizingHelper(Canvas canvas, RichTextBox rtb)
        {
            this.RTBCanvas = canvas;
            this.RTB = rtb;

            this.RTB.PreviewMouseMove += this.RTB_MouseMove;

        }

        ContainerVisual currentTableVisual;

        private bool IsTable(ContainerVisual visual)
        {

            foreach (Visual child in visual.Children)
            {
                if (child.GetType().Name == "RowVisual")
                    return true;
            }

            return false;

        }

        private void CreateGridWrapper(Table table, ContainerVisual tableVisual)
        {

            Grid grid = new Grid
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(this.RTB.Margin.Left + tableVisual.ContentBounds.Left + table.BorderThickness.Left,
                 this.RTB.Margin.Top + tableVisual.ContentBounds.Top + table.BorderThickness.Top, table.BorderThickness.Left, table.BorderThickness.Top),
                Width = tableVisual.ContentBounds.Width - (table.BorderThickness.Left + table.BorderThickness.Right),
                Height = tableVisual.ContentBounds.Height - (table.BorderThickness.Top + table.BorderThickness.Bottom),

            };

            foreach (TableColumn tc in table.Columns)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(tc.Width.Value + table.CellSpacing) });
            }


            grid.SetValue(Canvas.ZIndexProperty, 2);

            int rowIndex = 0;
            int column = 0;

            foreach (TableRowGroup rowGroup in table.RowGroups)
            {
                rowIndex = 0;

                bool[][] cellExistence = new bool[rowGroup.Rows.Count][];

                foreach (var row in rowGroup.Rows)
                {
                    
                    grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                    column = 0;

                    if (cellExistence[rowIndex] == null)
                    {
                        cellExistence[rowIndex] = new bool[table.Columns.Count];
                    }

                    foreach (var cell in row.Cells)
                    {

                        if (cell.RowSpan > 1)
                        {

                            Enumerable.Range(rowIndex + 1, cell.RowSpan - 1).ToList().ForEach(

                                ri =>
                                {
                                    if (cellExistence[ri] == null)
                                    {
                                        cellExistence[ri] = new bool[table.Columns.Count];
                                    }
                                    cellExistence[ri][column] = true;

                                }

                                );

                        }

                        if (cellExistence[rowIndex][column])
                            column++;

                        {

                            //Button b = new Button() { Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Stretch };
                            TextBlock b = new TextBlock() { Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Stretch };
                            b.SetValue(Grid.RowProperty, rowIndex);
                            b.SetValue(Grid.ColumnProperty, column);
                            b.SetValue(Grid.ColumnSpanProperty, cell.ColumnSpan);
                            b.SetValue(Grid.RowSpanProperty, cell.RowSpan);

                            grid.Children.Add(b);

                            GridSplitter gs = new GridSplitter
                            {
                                Tag = table,
                                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                                //HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                                Background = Brushes.Transparent,
                                Width = 2,
                                // BorderThickness=new Thickness(10),
                                Margin = new Thickness(0),
                                Padding = new Thickness(0),
                                ResizeDirection = GridResizeDirection.Auto
                            };

                            gs.DragCompleted += gs_DragCompleted;

                            gs.SetValue(Grid.RowProperty, rowIndex);
                            gs.SetValue(Grid.ColumnProperty, column);
                            gs.SetValue(Grid.ColumnSpanProperty, cell.ColumnSpan);
                            gs.SetValue(Grid.RowSpanProperty, cell.RowSpan);

                            grid.Children.Add(gs);
                        }

                        column++;
                    }


                    rowIndex++;
                }




            }



            RTBCanvas.Children.Add(grid);




        }

        private void gs_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            GridSplitter gs = sender as GridSplitter;

            if (gs != null)
            {

                int columnIndex = (int)gs.GetValue(Grid.ColumnProperty);

                Table table = gs.Tag as Table;
                if (table != null)
                {


                    if (e.HorizontalChange > 0 || Math.Abs(e.HorizontalChange) < table.Columns[columnIndex].Width.Value)
                    {
                        table.Columns[columnIndex].Width = new GridLength(table.Columns[columnIndex].Width.Value + e.HorizontalChange);
                    }

                    Grid grid = this.RTBCanvas.Children[this.RTBCanvas.Children.Count - 1] as Grid;

                    if (grid != null)
                    {
                        this.RTBCanvas.Children.Remove(grid);
                        grid.Visibility = Visibility.Hidden;
                    }

                }



            }

        }

        private List<ContainerVisual> GetParagraphVisuals()
        {
            DependencyObject border = VisualTreeHelper.GetChild(this.RTB, 0);

            DependencyObject scrollViewer = VisualTreeHelper.GetChild(border, 0);

            DependencyObject grid = VisualTreeHelper.GetChild(scrollViewer, 0);


            ScrollContentPresenter scp = VisualTreeHelper.GetChild(grid, 1) as ScrollContentPresenter;

            DependencyObject flowDocView = VisualTreeHelper.GetChild(scp, 0);

            DependencyObject pageVisual = VisualTreeHelper.GetChild(flowDocView, 0);

            DependencyObject containerVisual = VisualTreeHelper.GetChild(pageVisual, 0);

            DependencyObject anotherVisual = VisualTreeHelper.GetChild(containerVisual, 0);

            DependencyObject yetAnotherVisual = VisualTreeHelper.GetChild(anotherVisual, 0);

            int cc = VisualTreeHelper.GetChildrenCount(yetAnotherVisual);

            return Enumerable.Range(0, cc).Select(i => VisualTreeHelper.GetChild(yetAnotherVisual, i) as ContainerVisual).ToList();
        }

        private void RTB_MouseMove(object sender, MouseEventArgs e)
        {

            e.Handled = true;

            Point p = Mouse.GetPosition(this.RTB);

            List<Table> allTables = this.RTB.Document.Blocks.OfType<Table>().ToList();

            List<ContainerVisual> visuals = GetParagraphVisuals();

            List<ContainerVisual> tableVisuals = visuals.Where(IsTable).ToList();

            ContainerVisual tableVisual = tableVisuals.FirstOrDefault(
                tv => new Rect(tv.ContentBounds.TopLeft, tv.ContentBounds.BottomRight).Contains(p)
                );



            if (currentTableVisual == null || currentTableVisual != tableVisual)
            {

                Grid grid = this.RTBCanvas.Children[this.RTBCanvas.Children.Count - 1] as Grid;

                if (grid != null)
                {
                    this.RTBCanvas.Children.Remove(grid);
                    grid.Visibility = Visibility.Hidden;
                }

                currentTableVisual = tableVisual;

                if (tableVisual != null)
                {
                    int index = tableVisuals.FindIndex(tv => tv == tableVisual);
                    Table table = allTables.ElementAt(index) as Table;
                    if (table != null)
                        CreateGridWrapper(table, tableVisual);
                }
            }
        }

    
    }


}

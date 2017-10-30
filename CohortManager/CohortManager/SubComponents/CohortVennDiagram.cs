using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data.Aggregation;
using CatalogueLibrary.Data.Cohort;
using CohortManagerLibrary.Execution;
using MapsDirectlyToDatabaseTable;
using ReusableUIComponents.Heatmapping;
using ReusableUIComponents.Icons.IconProvision;

namespace CohortManager.SubComponents
{
    public partial class CohortVennDiagram : UserControl
    {
        private SetOperation _operationToRender = SetOperation.EXCEPT;
        private Color _fillColour = Color.Red;
        private IOrderable[] _contentsOfCohortContainer;
        
        private object oSetupLock = new object();
        private IIconProvider _iconProvider;
        private CohortCompiler _compiler;
        private CohortAggregateContainer _containerBeingRendered;

        public CohortVennDiagram()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            lock (oSetupLock)
            {
                DrawVenn(e.Graphics, 600);    
            }
            
        }

        public void SetupFor(IIconProvider iconProvider,CohortAggregateContainer container,CohortCompiler compiler)
        {
            _iconProvider = iconProvider;
            _containerBeingRendered = container;
            _compiler = compiler;

            lock (oSetupLock)
            {
                _operationToRender = container.Operation;
                _contentsOfCohortContainer = container.GetOrderedContents().ToArray();
            }

            Invalidate();
        }
        public void Clear()
        {
            lock (oSetupLock)
            {
                _containerBeingRendered = null;
                _contentsOfCohortContainer = null;
                
            }

            Invalidate();

        }

        private void DrawVenn(Graphics g,int size)
        {
            g.FillRectangle(Brushes.White,0,0,size,size);

            if(_contentsOfCohortContainer == null)
                return;

            int circleCount = _contentsOfCohortContainer.Length;

            //empty container
            if(circleCount == 0)
                return;

            var colourPicker = new RainbowColorPicker(circleCount);
            
            float middle = (float)size/2;
            float ringRadius = middle/3;
            float circleRadius = middle/2;

            Region intersection = new Region();

            var fillBrush = new SolidBrush(_fillColour);

            //how far around the centre point to increment after drawing each circle
            float angleOfIncrement = 360/(float)circleCount;

            List<RectangleF> circleRects = new List<RectangleF>();

            for (int i = 0; i < circleCount; i++)
            {
                float theta = angleOfIncrement*i;

                var x = (float)(middle + ringRadius * Math.Cos(135 + (theta * Math.PI) / 180));
                var y = (float)(middle + ringRadius * Math.Sin(135 + (theta * Math.PI) / 180));    
                 
                var rect = new RectangleF(x - circleRadius,y-circleRadius,2*circleRadius,2*circleRadius);
                
                //add the rect so we can draw the PEN bit afterwards
                circleRects.Add(rect);

                //DRAW THE FILLS:

                //everything is red with a UNION
                if(_operationToRender == SetOperation.UNION)
                    g.FillEllipse(fillBrush,rect);

                //fill first with red and rest with white for EXCEPT
                if(_operationToRender == SetOperation.EXCEPT)
                    g.FillEllipse( i == 0 ? fillBrush:Brushes.White, rect);
                
                using (GraphicsPath circlePath = new GraphicsPath())
                {
                    circlePath.AddEllipse(rect);
                    intersection.Intersect(circlePath);
                }
            }

            //middle bit only for INTERSECT
            if(_operationToRender == SetOperation.INTERSECT)
                g.FillRegion(fillBrush,intersection);

            //Draw the Circle boundaries labels etc
            for (int i = 0; i < circleRects.Count; i++)
            {
                Pen circlePen = new Pen(colourPicker.Colors[i]);

                RectangleF rect = circleRects[i];
                var contents = (IMapsDirectlyToDatabaseTable)_contentsOfCohortContainer[i];

                var taskExecution = _compiler.GetTaskIfExists(contents);
                
                string toRender = contents.ToString();
                var labelSize = g.MeasureString(toRender, Font);
                
                g.DrawEllipse(circlePen, rect);

                var img = _iconProvider.GetImage(contents);

                //draw the labels
                float labelX = 0;
                float labelY = rect.Top + (rect.Height/2);

                if (rect.Left + (rect.Width/2) < middle)
                {
                    //circle is on left hand side of diagram   
                    labelX = Math.Max(20,rect.Left - (labelSize.Width /2));

                }
                else
                {
                    //circle is on right hand side of diagram
                    labelX = Math.Min(size - labelSize.Width,rect.Right - (labelSize.Width / 2));
                }
                
                RectangleF labelRect = new RectangleF(new PointF(labelX,labelY),labelSize);
                g.FillRectangle(Brushes.White,labelRect);
                g.DrawRectangle(circlePen, Rectangle.Round(labelRect));
                g.DrawString(toRender,Font,Brushes.Black,labelX,labelY);
                g.DrawImage(img, labelX-19,labelY);

                if (taskExecution != null && taskExecution.State == CompilationState.Finished)
                {
                    var countToRender = string.Format("{0:n0}", taskExecution.FinalRowCount);
                    var countToRenderSize = g.MeasureString(countToRender, Font);

                    //center the count label under the set label
                    var countLabelX = labelX + (labelSize.Width/2) - (countToRenderSize.Width/2);
                    var countLabelY = labelY + countToRenderSize.Height;
                    RectangleF countLabelRect = new RectangleF(new PointF(countLabelX,countLabelY),countToRenderSize);

                    g.FillRectangle(Brushes.White,countLabelRect);
                    g.DrawRectangle(circlePen,Rectangle.Round(countLabelRect));
                    g.DrawString(countToRender,Font,Brushes.Black,countLabelRect);
                }
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Linq;

using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Attributes;
using Lab_Mouse.Properties;
using Lab_Mouse.Components;



namespace Lab_Mouse.Components
{
    public class PSlider : Grasshopper.Kernel.Special.GH_NumberSlider
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        /// 

        public List<double> probabilities;
        public float max;
        public float min;
        public string draw_flag;
        public PSlider()

          : base()
        {
            base.Name = "PDF Slider";
            base.NickName = "PSlider";
            base.Description = "bla bla ";
            base.Category = "Lab Mouse";
            base.SubCategory = "Modeling";

            this.probabilities = new List<double> {1, 0.5, 1, 0.5, 1, 0.5, 1, 0.5 }; // deafault starting distribution
           
            max = (float)(this.Slider.Maximum);
            min = (float)(this.Slider.Minimum);
            this.draw_flag = "h";
        }



        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        public override void CreateAttributes()
        {
            {
                this.m_attributes = (IGH_Attributes)new PSliderAttributes(this, probabilities);
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {

            //Menu_AppendSeperator(menu);
            menu.Items.Add("Histogram", null, menuItemHisto);
            //Menu_AppendSeperator(menu);
            menu.Items.Add("Smooth", null, menuItemSmooth);
            base.AppendAdditionalMenuItems(menu);

        }

        public void menuItemHisto(object sender, EventArgs e)
        {
            this.draw_flag = "h";
        }

        public void menuItemSmooth(object sender, EventArgs e)
        {
            this.draw_flag = "s";
        }

        // Call to update the PDF of this PSlider
        public void updatePDF(List<double> p)
        {
            this.probabilities = p;
        }
       

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Lab_Mouse.Properties.Resources.PSlider_icon;
                
                //return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("44a1a9c7-0eef-4cb1-b6de-c30522eb0aa6"); }
        }

    }

    // sets maximum height of histogram window above slider (global variable)
    static class glob
    {
        public static int max_ht = 50;
   
    }


    // this class overrides the classic slider to inherit it's properties and draw over it
    public class PSliderAttributes : Grasshopper.Kernel.Special.GH_NumberSliderAttributes
    {

        private List<double> probabilities;
        PSlider own;
        // private float minimum;

        public PSliderAttributes(PSlider owner, List<double> probabs) :
          base(owner)
        {

            probabilities = probabs;
            own = owner;
        }

        private float bins(float val)
        {

            List<List<float>> bin_ranges = new List<List<float>>();
            float max = (float)this.Owner.Slider.Maximum;
            float min = (float)this.Owner.Slider.Minimum;
            float bin_size = (max - min) / this.probabilities.Count;
            float returned_p = 0;

            float counter = min;
            for (int i = 0; i < this.probabilities.Count; i++)
            {

                List<float> t = new List<float> { counter, counter + bin_size };
                bin_ranges.Add(t);

                counter = t[1];
            }


            for (int i = 0; i < bin_ranges.Count; i++)
            {
                if (val > bin_ranges[i][0] && val <= bin_ranges[i][1])
                {
                    returned_p = (float)this.probabilities[i];
                }
            }
            return returned_p;
        }

        // this function gets the coordinates from the probabilities, for drawing an irregular polygon from points 
        private PointF[] getPts(List<double> Probabilities)

        {


            int n = probabilities.Count + 4;
            PointF[] points = new PointF[n];

            int width_nickname = GH_FontServer.StringWidth(Owner.NickName, GH_FontServer.Standard);
        

            points[0] = new PointF(this.Pivot.X + width_nickname + 19, this.Pivot.Y - 7);
            points[1] = new PointF(this.Pivot.X + (Bounds.Width) - 10, this.Pivot.Y - 7);


            if (probabilities.Count != 0)
            {
                // routine to get drawing coordinates based on bin  probabilities
                for (int i = 0; i < Probabilities.Count; i++)
                {
                    float rail_width = (int)(points[1].X - points[0].X);
                    float bin_width = rail_width / Probabilities.Count;
                    float t = (Probabilities.Count - i) / Probabilities.Count;

                    points[i + 3] = new PointF((float)((this.Pivot.X + width_nickname + 19) + (bin_width * (Probabilities.Count - i)) - bin_width * 0.5), this.Pivot.Y - 7 - (glob.max_ht * (float)Probabilities[Probabilities.Count - i - 1]));
                }
            }

            points[2] = new PointF(points[1].X, points[3].Y);
            points[n - 1] = new PointF(points[0].X, points[n - 2].Y);

            return points;
        }

        
        // this function gets the coordinates from the probabilities, (same as getPts) but draws a HISTOGRAM shape an irregular polygon from points
        private PointF[] getHistoPts(List<double> Probabilities)

        {
            int n = (Probabilities.Count * 2) + 2;
            PointF[] points = new PointF[n];

            int width_nickname = GH_FontServer.StringWidth(Owner.NickName, GH_FontServer.Standard);
     

            points[0] = new PointF(this.Pivot.X + width_nickname + 19, this.Pivot.Y - 7);
            points[1] = new PointF(this.Pivot.X + (Bounds.Width) - 10, this.Pivot.Y - 7);


            if (probabilities.Count != 0)
            {
                int count = 0;
                // routine to get drawing coordinates based on bin  probabilities
                for (int i = 0; i < Probabilities.Count; i++)
                {
                    float rail_width = (int)(points[1].X - points[0].X);
                    float bin_width = rail_width / Probabilities.Count;
                    float t = (Probabilities.Count - i) / Probabilities.Count;

                    points[count + 2] = new PointF((float)((this.Pivot.X + width_nickname + 19) + (bin_width * (Probabilities.Count - i))), this.Pivot.Y - 7 - (glob.max_ht * (float)Probabilities[Probabilities.Count - i - 1]));
                    points[count + 2 + 1] = new PointF((float)((this.Pivot.X + width_nickname + 19) + (bin_width * (Probabilities.Count - i - 1))), this.Pivot.Y - 7 - (glob.max_ht * (float)Probabilities[Probabilities.Count - i - 1]));
                    count += 2;
                }
            }

            return points;
        }


        // Rui 
        // double click handler on graph
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            System.Drawing.RectangleF rec = GraphBounds;
            if (rec.Contains(e.CanvasLocation))
            {
                Rhino.RhinoApp.WriteLine("double clicked!!!!");
                // Rui
                // update possibility 
                // todo : mock procedure for now
                List<double> newProbabilities = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
                List<double> oldProbabilities = new List<double> { 1, 0.5, 1, 0.5, 1, 0.5, 1, 0.5 };
                if (probabilities.SequenceEqual(newProbabilities))
                {
                    probabilities = oldProbabilities;
                }
                else
                {
                    probabilities = newProbabilities;
                }
                return GH_ObjectResponse.Handled;
            }
            Rhino.RhinoApp.WriteLine("not double clicked!!!!");
            return base.RespondToMouseDoubleClick(sender, e);
        }

        // Rui
        // new layout 
        protected override void Layout()
        {
            base.Layout();

            System.Drawing.Rectangle rec0 = GH_Convert.ToRectangle(Bounds);
            rec0.Height += 60;
            rec0.Y -= 60;

            System.Drawing.Rectangle rec1 = rec0;
            rec1.Y = rec1.Bottom - 80;
            rec1.Height = 60;

            Bounds = rec0;
            GraphBounds = rec1;
        }

        // Rui
        // Graph Bounds
        private System.Drawing.Rectangle GraphBounds { get; set; }


        // this function takes care of the drawing routines 
        protected override void Render(Grasshopper.GUI.Canvas.GH_Canvas canvas, Graphics graphics,
          Grasshopper.GUI.Canvas.GH_CanvasChannel channel)
        {

            if (channel != Grasshopper.GUI.Canvas.GH_CanvasChannel.Objects)
                return;

            // Rui
            // render addtional graph
            if (channel == GH_CanvasChannel.Objects)
            {
                GH_Capsule Graph = GH_Capsule.CreateCapsule(GraphBounds, GH_Palette.Grey);
                GH_Capsule Wrapper = GH_Capsule.CreateCapsule(Bounds, GH_Palette.Grey);

                Graph.Render(graphics, Selected, Owner.Locked, false);
                Wrapper.Render(graphics, Selected, Owner.Locked, false);

                Graph.Dispose();
                Wrapper.Dispose();
            }

            // render of original component 
            base.Render(canvas, graphics, channel);

            //Attribues.Pivot = new PointF(0, 10);
            int width = GH_FontServer.StringWidth(Owner.NickName, GH_FontServer.Standard);
            PointF p = new PointF(this.Pivot.X + width + 19, this.Pivot.Y - 7);
            //width = Math.Max(width + 10, 80)

            List<double> probs = this.probabilities;

            PointF[] pts = getHistoPts(probs);

            if (own.draw_flag == "s")
            {
                pts = getPts(probs);
            }

            PointF maxPt = new PointF(0, 0);

            /*
            double m = probs.Max();
            for (int i = 0; i < probs.Count; i++)
            {
              double d = probs[i];
              if (d == m){
                //maxPt = pts[i];
                maxPt = pts[2 + (probs.Count - 1 - i)];
              }
            }
            */

            float ptY = pts[0].Y;
            int index_max = 0;
            for (int i = 0; i < pts.Length; i++)
            {
                if (pts[i].Y < ptY)
                {
                    ptY = pts[i].Y;
                    index_max = i;
                }
            }

            maxPt = pts[index_max];

        
            // Create Pens
            System.Drawing.Pen pen2 = new System.Drawing.Pen(Brushes.Red, 2);
            System.Drawing.Pen pen = new System.Drawing.Pen(Brushes.Black, 2);
            System.Drawing.Pen pen3 = new System.Drawing.Pen(Brushes.GhostWhite, 1);

            // Create Gradient Brush
            System.Drawing.Drawing2D.LinearGradientBrush lb = new System.Drawing.Drawing2D.LinearGradientBrush
              (new PointF(maxPt.X, maxPt.Y), new PointF(maxPt.X, this.Pivot.Y), Color.FromArgb(255, 0, 0),
              Color.FromArgb(255, 255, 255));
            System.Drawing.SolidBrush sb = new System.Drawing.SolidBrush(Color.FromArgb(70, 255, 255, 255));

            // Draw background (write in function?)
            int width_nickname = GH_FontServer.StringWidth(Owner.NickName, GH_FontServer.Standard);
            Rectangle background = new Rectangle((int)(this.Pivot.X + width_nickname + 19), (int)(this.Pivot.Y - 8 - glob.max_ht), (int)(pts[1].X - pts[0].X), glob.max_ht);

            graphics.DrawRectangle(pen3, background);
            graphics.FillRectangle(sb, background);

            //Draw Polygon ouline and fill
            graphics.DrawPolygon(pen, pts);
            graphics.FillPolygon(lb, pts);

            //Owner.NickName = "Variable";

            // Draw probability value
            // string evidence = "Y<5

            string s = "P(MINIMIZE Y)=" + (this.bins((float)Owner.CurrentValue) * 100).ToString() + "%";
            //string s = (Owner.CurrentValue + 1).ToString();
            //double cv = Owner.CurrentValue;
            graphics.DrawString(s, GH_FontServer.Standard, Brushes.Black, pts[1].X+15, (int)(this.Pivot.Y - 8 - glob.max_ht));



        }

    }

}

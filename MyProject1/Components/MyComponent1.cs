using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using GH_IO.Serialization;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;

namespace Testing
{
    public class SuperDuperComponentAtteributes : GH_ComponentAttributes
    {
        public SuperDuperComponentAtteributes(SuperDuperComponent owner)
          : base(owner)
        { }

        private RectangleF _baseBounds;
        private RectangleF _thisBounds;
        private RectangleF _buttonBounds;

        protected override void Layout()
        {
            base.Layout();
            _baseBounds = Bounds;

            _buttonBounds = Bounds;
            _buttonBounds.Y = Bounds.Bottom + 10;
            _buttonBounds.Height = 32;

            _thisBounds = RectangleF.Union(_baseBounds, _buttonBounds);

            // Overwrite the Bounds property to include our external button.
            Bounds = _thisBounds;
        }
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            // Re-instate the Bounds computed by the base class Layout method.
            // But only during calls to the base class.
            Bounds = _baseBounds;
            base.Render(canvas, graphics, channel);
            Bounds = _thisBounds;

            if (channel == GH_CanvasChannel.Objects)
            {
                string text;
                if ((Owner as SuperDuperComponent).MinMax)
                    text = "MinMax";
                else
                    text = "MaxMin";

                GH_Capsule button = GH_Capsule.CreateTextCapsule(_buttonBounds, _buttonBounds, GH_Palette.Pink, text);
                button.Render(graphics, Selected, Owner.Locked, Owner.Hidden);
            }
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (_buttonBounds.Contains(e.CanvasLocation))
            {
                // If the double-click happened on our button, we need to handle the event.
                SuperDuperComponent sd = Owner as SuperDuperComponent;
                if (sd == null)
                    return GH_ObjectResponse.Ignore;

                sd.RecordUndoEvent("MinMax toggle");
                sd.MinMax = !sd.MinMax;
                sd.ExpireSolution(true);
                return GH_ObjectResponse.Handled;
            }

            // If not, we need to let the base class handle the event.
            // Just to make sure the base class doesn't get confused, we should once again
            // pretend that the Bounds are as expected.
            Bounds = _baseBounds;
            GH_ObjectResponse rc = base.RespondToMouseDoubleClick(sender, e);
            Bounds = _thisBounds;
            return rc;
        }
    }

    public class SuperDuperComponent : GH_Component
    {
        public SuperDuperComponent()
          : base("Super Duper", "SupDup", "Tinned awesomeness, consume before 6/2018", "Test", "Test")
        {
            MinMax = true;
        }
        public override void CreateAttributes()
        {
            m_attributes = new SuperDuperComponentAtteributes(this);
        }

        public static readonly Guid SuperDuperId = new Guid("{0CD86E8B-FA36-47A5-9D71-9C678F96B13A}");
        public override Guid ComponentGuid
        {
            get { return SuperDuperId; }
        }
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Gets or sets whether the logic ought to be MinMax or MaxMin.
        /// </summary>
        public bool MinMax { get; set; }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Gary", "G", "Who the man?", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Cooper", "C", "Damn right!", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Super", "S", "Million dollar trooper", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Duper", "D", "Puttin' on the Ritz", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int a = 0;
            int b = 0;
            if (!DA.GetData(0, ref a)) return;
            if (!DA.GetData(1, ref b)) return;

            if (MinMax)
            {
                DA.SetData(0, Math.Min(a, b));
                DA.SetData(1, Math.Max(a, b));
            }
            else
            {
                DA.SetData(0, Math.Max(a, b));
                DA.SetData(1, Math.Min(a, b));
            }
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("MinMax", MinMax);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            MinMax = reader.GetBoolean("MinMax");
            return base.Read(reader);
        }
    }
}

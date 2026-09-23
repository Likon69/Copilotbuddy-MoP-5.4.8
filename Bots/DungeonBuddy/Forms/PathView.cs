using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Bots.DungeonBuddy.Avoidance;
using Styx;
using Tripper.Tools.Math;

namespace Bots.DungeonBuddy.Forms;

public class PathView : Form
{
	private WoWPoint _myLoc;

	private float _zoomRatio = 4f;

	private Graphics gfx;

	private PointF _centerPos;

	private Bitmap _renderedImage;

	private Bitmap _gfxBitMap;

	private bool _isDragging;

	private Point _lastMoustPoint;

	private IContainer components;

	private Timer timer1;

	public static PathView Instance { get; private set; }

	public PathView()
	{
		Instance = this;
		InitializeComponent();
		_gfxBitMap = new Bitmap(base.Width, base.Height);
		gfx = Graphics.FromImage(_gfxBitMap);
		gfx.SmoothingMode = SmoothingMode.HighQuality;
		base.MouseWheel += PathViewMouseWheel;
		timer1.Enabled = true;
		timer1.Start();
		timer1.Interval = 100;
		_centerPos = new PointF((float)base.Width / 2f, (float)base.Height / 2f);
	}

	private void PathViewMouseWheel(object sender, MouseEventArgs e)
	{
		int delta = e.Delta / 120;
		if (delta < 0 && _zoomRatio > 1f)
		{
			_zoomRatio -= 0.2f;
		}
		if (delta > 0 && _zoomRatio < 6f)
		{
			_zoomRatio += 0.2f;
		}
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		_gfxBitMap = new Bitmap(base.Width, base.Height);
		gfx = Graphics.FromImage(_gfxBitMap);
		gfx.SmoothingMode = SmoothingMode.HighQuality;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (_renderedImage != null)
		{
			e.Graphics.DrawImage(_renderedImage, 0, 0);
		}
		else
		{
			e.Graphics.Clear(BackColor);
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (!_isDragging)
			{
				_isDragging = true;
				_lastMoustPoint = e.Location;
				return;
			}
			int dx = e.X - _lastMoustPoint.X;
			int dy = e.Y - _lastMoustPoint.Y;
			_centerPos.X += dy;
			_centerPos.Y += dx;
			_lastMoustPoint = e.Location;
		}
		else if (_isDragging)
		{
			_isDragging = false;
		}
	}

	private void Timer1Tick(object sender, EventArgs e)
	{
		try
		{
			gfx.Clear(BackColor);
			_myLoc = StyxWoW.Me.Location;
			DungeonNavigationProvider navigationProvider = DungeonBot.NavigationProvider;
			Vector3[] array = ((navigationProvider != null && navigationProvider.CurrentAvoidPath != null) ? navigationProvider.CurrentAvoidPath.Path : ((navigationProvider.CurrentMovePath != null) ? navigationProvider.CurrentMovePath.Path.Points : null));
			foreach (Avoid avoid in AvoidanceManager.Avoids)
			{
				DrawCircle(Color.MediumPurple, Color.MediumPurple, avoid.Location, avoid.Radius, 1);
			}
			if (array != null)
			{
				if (array.Length == 1)
				{
					DrawPoint(Color.Red, navigationProvider.CurrentMovePath.Path.Points[0], 2f);
					DrawLine(Color.Black, _myLoc, navigationProvider.CurrentMovePath.Path.Points[0], 1);
				}
				else
				{
					for (int i = 1; i < array.Length; i++)
					{
						DrawPoint(Color.Red, array[i], 2f);
						DrawLine(Color.Black, array[i - 1], array[i], 1);
					}
				}
			}
			WoWPoint end = _myLoc.RayCast(StyxWoW.Me.Rotation, 10f);
			DrawPoint(Color.Green, _myLoc, 3f);
			DrawLine(Color.Green, _myLoc, end, 1);
			RenderImage(_gfxBitMap);
		}
		catch
		{
		}
	}

	private PointF WoWToScreen(WoWPoint point)
	{
		float centerX = _centerPos.X;
		float centerY = _centerPos.Y;
		point.X = (_myLoc.X - point.X) * _zoomRatio;
		point.Y = (_myLoc.Y - point.Y) * _zoomRatio;
		return new PointF(point.Y + centerY, point.X + centerX);
	}

	private void DrawCircle(Color color, Color fillColor, WoWPoint circlePos, float radius, int thickness)
	{
		Pen pen = new Pen(color, thickness);
		SolidBrush fillBrush = new SolidBrush(fillColor);
		DrawCircle(pen, fillBrush, circlePos, radius);
	}

	private void DrawCircle(Pen pen, Brush fillBrush, WoWPoint circlePos, float radius)
	{
		PointF pointF = WoWToScreen(circlePos);
		float diameter = radius * 2f * _zoomRatio;
		gfx.DrawEllipse(pen, pointF.X - radius * _zoomRatio, pointF.Y - radius * _zoomRatio, diameter, diameter);
		gfx.FillEllipse(fillBrush, pointF.X - radius * _zoomRatio, pointF.Y - radius * _zoomRatio, diameter, diameter);
	}

	private void DrawPoint(Color color, WoWPoint loc, float radius)
	{
		SolidBrush brush = new SolidBrush(color);
		PointF pointF = WoWToScreen(loc);
		float diameter = radius * 2f;
		gfx.FillEllipse(brush, pointF.X - radius, pointF.Y - radius, diameter, diameter);
	}

	private void DrawLine(Color color, WoWPoint start, WoWPoint end, int thickness)
	{
		Pen pen = new Pen(color, thickness);
		DrawLine(pen, start, end);
	}

	private void DrawLine(Pen pen, WoWPoint start, WoWPoint end)
	{
		PointF pt = WoWToScreen(start);
		PointF pt2 = WoWToScreen(end);
		gfx.DrawLine(pen, pt, pt2);
	}

	private void DrawPolygon(Color color, Color fillColor, int thickness, params WoWPoint[] points)
	{
		Pen pen = new Pen(color, thickness);
		SolidBrush fillBrush = new SolidBrush(fillColor);
		DrawPolygon(pen, fillBrush, points);
	}

	private void DrawPolygon(Pen pen, Brush fillBrush, params WoWPoint[] points)
	{
		PointF[] points2 = points.Select(WoWToScreen).ToArray();
		gfx.DrawPolygon(pen, points2);
		if (fillBrush != null)
		{
			gfx.FillPolygon(fillBrush, points2);
		}
	}

	public void RenderImage(Bitmap img)
	{
		if (_renderedImage != null)
		{
			_renderedImage.Dispose();
		}
		_renderedImage = new Bitmap(base.Width, base.Height);
		Graphics graphics = Graphics.FromImage(_renderedImage);
		graphics.Clear(BackColor);
		graphics.DrawImage(img, 0, 0, base.Width, base.Height);
		graphics.Dispose();
		Refresh();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this.timer1 = new System.Windows.Forms.Timer(this.components);
		base.SuspendLayout();
		this.timer1.Tick += new System.EventHandler(Timer1Tick);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(784, 761);
		base.Name = "PathView";
		this.Text = "PathView";
		base.ResumeLayout(false);
	}
}

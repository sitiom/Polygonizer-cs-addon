#if TOOLS
using Godot;
using System.Collections.Generic;
using System.Linq;
using Array = Godot.Collections.Array;

[Tool]
public class PolygonizerPlugin : EditorPlugin
{
	private ToolButton _button;
	private ToolButton _settingsButton;
	private ConfirmationDialog _settingsDialog;
	private bool _enablePreview;

	private Polygonizer _polygonizer;
	private readonly List<Node2D> _previews = new List<Node2D>();
	
	public override void _EnterTree()
	{
		_settingsButton = new ToolButton { Text = "Polygonizer Settings" };
		_settingsButton.Connect("pressed", this, nameof(Settings_ButtonClicked));
		_button = new ToolButton { Text = "Polygonize" };
		_button.Connect("pressed", this, nameof(Polygonize_ButtonClicked));
		_button.Hide();
		_settingsDialog = (ConfirmationDialog)ResourceLoader.Load<PackedScene>("addons/Polygonizer/SettingsDialog.tscn").Instance();
		GetEditorInterface().GetBaseControl().AddChild(_settingsDialog);
		GetEditorInterface().GetInspector().Connect("property_edited", this, nameof(OnPropertyEdited));
		AddControlToContainer(CustomControlContainer.CanvasEditorMenu, _settingsButton);
		AddControlToContainer(CustomControlContainer.CanvasEditorMenu, _button);
		GetEditorInterface().GetSelection().Connect("selection_changed", this, nameof(OnSelectionChanged));
		_settingsDialog.Connect("confirmed", this, nameof(Settings_DialogConfirmed));
	}

	private void OnPropertyEdited(string property)
	{
		bool nonSpriteExists = GetEditorInterface().GetSelection().GetSelectedNodes().Cast<Node>()
			.Any(n => !(n is Sprite));
		if (nonSpriteExists) return;

		UpdatePreviews();
	}

	private void OnSelectionChanged()
	{
		UpdatePreviews();
		if (GetEditorInterface().GetSelection().GetSelectedNodes().Count == 0)
		{
			_button.Hide();
		}
		else
		{
			bool nonSpriteExists = GetEditorInterface().GetSelection().GetSelectedNodes().Cast<Node>()
				.Any(n => !(n is Sprite));
			if (nonSpriteExists)
			{
				_button.Hide();
				return;
			}
			_button.Show();
		}
	}

	private void Settings_ButtonClicked()
	{
		_settingsDialog.GetNode<SpinBox>("VBoxContainer/HBoxContainer/ResolutionSpinBox").Value = _polygonizer.Resolution;
		_settingsDialog.GetNode<SpinBox>("VBoxContainer/HBoxContainer2/MarginSpinBox").Value = _polygonizer.Margin;
		_settingsDialog.GetNode<CheckBox>("VBoxContainer/PreviewCheckBox").Pressed = _enablePreview;
		_settingsDialog.PopupCentered();
	}

	private void Settings_DialogConfirmed()
	{
		_polygonizer.Resolution = _settingsDialog.GetNode<SpinBox>("VBoxContainer/HBoxContainer/ResolutionSpinBox").Value;
		_polygonizer.Margin = _settingsDialog.GetNode<SpinBox>("VBoxContainer/HBoxContainer2/MarginSpinBox").Value;
		_enablePreview = _settingsDialog.GetNode<CheckBox>("VBoxContainer/PreviewCheckBox").Pressed;
		UpdatePreviews();
	}

	private void Polygonize_ButtonClicked()
	{

		List<CollisionPolygon2D> addedPolygons = new List<CollisionPolygon2D>();
		Array selectedNodes = GetEditorInterface().GetSelection().GetSelectedNodes();
		GetEditorInterface().GetSelection().Clear();

		foreach (Sprite sprite in selectedNodes.OfType<Sprite>())
		{
			List<Vector2> polygon = _polygonizer.Scan(sprite);
			CollisionPolygon2D polygon2D = new CollisionPolygon2D();
			Node parent = sprite.GetParent();

			int targetNodePosition = parent.GetChildren().Cast<Node>().ToList().FindIndex(n => n == sprite);

			polygon2D.Polygon = polygon.ToArray();

			polygon2D.Position = GetTargetPolygonGlobalPosition(sprite);
			polygon2D.Rotation = sprite.Rotation;
			polygon2D.Scale = sprite.Scale;
			polygon2D.Name = "CollisionPolygon2D";
			parent.AddChild(polygon2D);
			parent.MoveChild(polygon2D, targetNodePosition + 1);
			polygon2D.Owner = GetTree().EditedSceneRoot;
			addedPolygons.Add(polygon2D);
		}

		if (addedPolygons.Count > 0)
		{
			foreach (CollisionPolygon2D node in addedPolygons)
			{
				GetEditorInterface().GetSelection().AddNode(node);
				node.Update();
			}
		}
	}

	public override void _ExitTree()
	{
		_button.QueueFree();
		_settingsDialog.QueueFree();
		_settingsButton.QueueFree();
		foreach (Node2D preview in _previews)
		{
			preview.QueueFree();
		}
	}

	private void UpdatePreviews()
	{
		foreach (Node2D preview in _previews)
		{
			preview.QueueFree();
		}
		_previews.Clear();

		if (!_enablePreview) return;

		foreach (Sprite sprite in GetEditorInterface().GetSelection().GetSelectedNodes().OfType<Sprite>())
		{
			List<Vector2> polygon = _polygonizer.Scan(sprite);
			PolygonPreview previewNode = (PolygonPreview)ResourceLoader.Load<PackedScene>("addons/Polygonizer/PolygonPreview.tscn").Instance();
			sprite.AddChild(previewNode);
			previewNode.SetPolygon(polygon);
			previewNode.GlobalPosition = GetTargetPolygonGlobalPosition(sprite);
			_previews.Add(previewNode);
		}
	}

	private Vector2 GetTargetPolygonGlobalPosition(Sprite sprite)
	{
		Vector2 targetGlobalPosition = sprite.GlobalPosition;

		if (sprite.Centered)
		{
			targetGlobalPosition -= new Vector2(sprite.Texture.GetWidth() / sprite.Hframes,
				sprite.Texture.GetHeight() / sprite.Vframes) / 2;
			targetGlobalPosition -=
				new Vector2(sprite.Texture.GetWidth() / sprite.Hframes,
					sprite.Texture.GetHeight() / sprite.Vframes) * sprite.FrameCoords;
		}

		return targetGlobalPosition;
	}
}
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Plugins
{
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	[RequireComponent(typeof(Collider2D))]
	public sealed class UICollider2DMeshGraphic : MaskableGraphic, ILayoutElement
	{
		[SerializeField] private Texture texture;
		[SerializeField] private float pixelsPerUnit = 100f;

		private readonly List<Vector3> vertices = new();
		private readonly List<Color32> colors = new();
		private readonly List<int> indices = new();
		private readonly List<Vector2> uv = new();

		private Collider2D polygon;
		private uint shapeHash;
		private Mesh mesh;

		public override Texture mainTexture => texture ? texture : s_WhiteTexture;

		protected override void OnEnable()
		{
			base.OnEnable();
			EnsureMesh();
		}

		protected override void OnDestroy()
		{
			DestroyOwnedMesh();
			base.OnDestroy();
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
		}

		protected override void OnDidApplyAnimationProperties()
		{
			base.OnDidApplyAnimationProperties();
			EnsureMesh();
		}

		private void OnTransformChildrenChanged()
		{
			if (polygon != null && polygon is CompositeCollider2D composite)
			{
				EnsureMesh();
			}
		}

		private void Update()
		{
			EnsureMesh();
		}

		private void EnsureMesh()
		{
			EnsureInit();

			uint newShapeHash = polygon.GetShapeHash();

			if (mesh == null || newShapeHash != shapeHash)
			{
				RebuildMesh();
			}

			if (mesh != null)
			{
				canvasRenderer.SetMaterial(materialForRendering, mainTexture);
				canvasRenderer.SetMesh(mesh);
			}
		}

		private void EnsureInit()
		{
			if (polygon == null)
				polygon = GetComponent<Collider2D>();
		}

		private void RebuildMesh()
		{
			if (polygon == null)
				return;

			if (mesh == null)
			{
				mesh = new Mesh
				{
					name = $"{nameof(UICollider2DMeshGraphic)} Mesh",
					hideFlags = HideFlags.HideAndDontSave
				};

				mesh.indexFormat = IndexFormat.UInt16;
			}

			shapeHash = polygon.GetShapeHash();

			Mesh sourceMesh = null;

			try
			{
				sourceMesh = polygon.CreateMesh(false, false, true);
				if (sourceMesh == null)
				{
					mesh.Clear();
					canvasRenderer.SetMesh(mesh);
					return;
				}

				BuildOwnedMesh(sourceMesh);
				canvasRenderer.SetMaterial(materialForRendering, mainTexture);
				canvasRenderer.SetMesh(mesh);
				SetMaterialDirty();
				SetVerticesDirty();
			}
			finally
			{
				DestroyTempMesh(sourceMesh);
			}
		}

		private void BuildOwnedMesh(Mesh sourceMesh)
		{
			mesh.Clear();

			sourceMesh.GetVertices(vertices);
			ConvertVerticesWorldToLocal(vertices);

			if (vertices.Count > 65534)
			{
				Debug.LogError(
					$"{nameof(UICollider2DMeshGraphic)}: mesh has {vertices.Count} vertices. " +
					$"CanvasRenderer expects 16-bit indices.",
					this);

				mesh.Clear();
				return;
			}

			sourceMesh.GetIndices(indices, 0);

			BuildFlatUv(vertices, uv, Mathf.Max(0.0001f, pixelsPerUnit));
			BuildColors(vertices.Count, color, color, colors);

			mesh.indexFormat = IndexFormat.UInt16;
			mesh.SetVertices(vertices);
			mesh.SetIndices(indices, MeshTopology.Triangles, 0, true);
			mesh.SetUVs(0, uv);
			mesh.SetColors(colors);
			mesh.RecalculateBounds();
		}

		private void ConvertVerticesWorldToLocal(List<Vector3> targetVertices)
		{
			Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

			for (int i = 0; i < targetVertices.Count; i++)
				targetVertices[i] = worldToLocal.MultiplyPoint3x4(targetVertices[i]);
		}

		private static void BuildFlatUv(List<Vector3> sourceVertices, List<Vector2> outputUv, float ppu)
		{
			outputUv.Clear();

			float invPpu = 1f / ppu;
			for (int i = 0; i < sourceVertices.Count; i++)
			{
				Vector3 v = sourceVertices[i];
				outputUv.Add(new Vector2(v.x * invPpu, v.y * invPpu));
			}
		}

		private static void BuildColors(int count, Color baseColor, Color graphicColor, List<Color32> outputColors)
		{
			outputColors.Clear();
			Color32 finalColor = (Color32)(baseColor * graphicColor);

			for (int i = 0; i < count; i++)
				outputColors.Add(finalColor);
		}

		private void DestroyOwnedMesh()
		{
			if (mesh == null)
				return;

			if (Application.isPlaying)
				Destroy(mesh);
			else
				DestroyImmediate(mesh);

			mesh = null;
		}

		private static void DestroyTempMesh(Mesh tempMesh)
		{
			if (tempMesh == null)
				return;

			if (Application.isPlaying)
				Destroy(tempMesh);
			else
				DestroyImmediate(tempMesh);
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			pixelsPerUnit = Mathf.Max(0.0001f, pixelsPerUnit);

			if (!isActiveAndEnabled)
				return;

			EnsureInit();
			RebuildMesh();
		}
#endif
		public void CalculateLayoutInputHorizontal()
		{
			EnsureInit();

			uint newShapeHash = polygon.GetShapeHash();
			if (mesh != null && newShapeHash == shapeHash)
			{
				return;
			}

			RebuildMesh();
			return;
		}

		public void CalculateLayoutInputVertical() { }

		public float minWidth { get; }
		public float preferredWidth => mesh.bounds.size.x;
		public float flexibleWidth { get; }
		public float minHeight { get; }
		public float preferredHeight => mesh.bounds.size.y;
		public float flexibleHeight { get; }
		public int layoutPriority { get; }
	}
	#if UNITY_EDITOR
	[CustomEditor(typeof(UICollider2DMeshGraphic))]
	public class PolygonColliderGraphicEditor : UnityEditor.Editor
	{
		private static readonly string[] _skipped = { "m_OnCullStateChanged" };

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, _skipped);
			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}
		}

		protected internal static void DrawPropertiesExcluding(
			SerializedObject obj,
			params string[] propertyToExclude)
		{
			SerializedProperty iterator = obj.GetIterator();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (iterator.name == "m_Script")
				{
					GUI.enabled = false;
				}

				if (!propertyToExclude.Contains(iterator.name))
				{
					EditorGUILayout.PropertyField(iterator, true);
				}

				if (iterator.name == "m_Script")
				{
					GUI.enabled = true;
				}
			}
		}
	}
	#endif
}

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;

public class PKMMaker : EditorWindow {
	
	const int EYEBROW 	= 0;
	const int FACE		= 1;
	const int MOUTH_DEF = 2;
	const int BODY 		= 3;
	const int EYE 		= 5;
	const int FACE_BACK = 15;
	const int CLOTHES 	= 0;
	const int EAR 		= 0;
	const int HAIR_B 	= 1;
	const int HAIR_F 	= 2;
	const int HAIR_S 	= 3;
	const int TAIL 		= 4;
	const int EAR_C		= 5;
	const int TEX_CLOTHES 		= 0;
	const int TEX_EYE_COLOR	 	= 1;
	const int TEX_EYEBROW	 	= 2;
	const int TEX_FACE		 	= 3;
	const int TEX_FACE_BACK	 	= 4;
	const int TEX_COMMON 		= 5;
	const int TEX_EAR			= 0;
	const int TEX_HAIR_B		= 1;
	const int TEX_HAIR_F		= 2;
	const int TEX_HAIR_S		= 3;
	const int TEX_TAIL			= 4;
	const int TEX_EAR_C			= 5;


	enum 衣服 {巫女服,ゴスロリ服,アイドル服,厚着,セーラー服,メイド服,水着}
	enum 眉 {眉1,眉2,眉3,眉4,眉5}
	enum 顔 {普通顔,ジト目顔,たれ目顔}
	enum 目色 {青,黄,赤}
	enum 口 {普通,にっこり,ω}
	enum 耳 {猫耳,狐耳,兎耳}
	enum 後髪 {長髪,短髪,ツインロール,おさげ1,ツインテール,おさげ2,外ハネ,ポニーテール}
	enum 前髪 {普通1,一本垂らし,普通2,短い,目隠れ,目隠れ2,左右分け}
	enum 横髪 {普通,長い,短い,ふっくら,結び,外ハネ}
	enum 尻尾 {猫尻尾,狐尻尾,兎尻尾}
	enum 髪色 {紺,黄,白}

	衣服 e_clothes;
	眉 e_eyebrow;
	顔 e_face;
	耳 e_ear;
	口 e_mouth_def;
	後髪 e_hair_b;
	前髪 e_hair_f;
	横髪 e_hair_s;
	尻尾 e_tail;
	目色 e_eye;
	髪色 e_hair_color;
	//毛色個別設定用
	髪色 e_eyebrow_color;
	髪色 e_ear_color;
	髪色 e_hair_b_color;
	髪色 e_hair_f_color;
	髪色 e_hair_s_color;
	髪色 e_tail_color;
	Color c_eyebrow_color = Color.white;
	Color c_ear_color = Color.white;
	Color c_hair_b_color = Color.white;
	Color c_hair_f_color = Color.white;
	Color c_hair_s_color = Color.white;
	Color c_tail_color = Color.white;

	Color clothesColor = Color.white;
	Color hairColor = Color.white;
	Color eyeColor = Color.white;
	bool disableClothes = false;
	bool eachColorSetting;
	bool randomizePKM;

	String savePath;
	bool isReset;

	Vector2 _scrollPosition = Vector2.zero;
	String texturePath = "Assets/PKMMaker/Texture/";
	String shader = "MMS/Mnmrshader1_3";
	GameObject model;

	//結合メッシュ
	SkinnedMeshRenderer[] body;
	SkinnedMeshRenderer[] clothes;
	SkinnedMeshRenderer[] hair;

	//結合テクスチャ
	Texture2D[] bodyTex;
	Texture2D[] clothesTex;
	Texture2D[] hairTex;

	//ルートボーン
	Transform rootBone;

	//--------------------------------------------------------------------------
	//あとやることリスト
	//・ランダム生成？
	//・アイテム変更追加
	//・ダイナミックボーンの出力オンオフ
	//・PrefabのルートにAvatarDescripter入れてそのまま使えるようにしたい
	//--------------------------------------------------------------------------

	[MenuItem("Tool/PKMMaker")]
	static void Open() {
		GetWindow<PKMMaker> ();
	}

	void OnEnable() {
		titleContent = new GUIContent ("ぷちけもメーカー");

		GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath ("Assets/PKMMaker/Models/Prefab/PKMCustom.prefab", typeof(GameObject));
		model = Instantiate (prefab);
		model.hideFlags = HideFlags.HideInHierarchy;
		foreach (SkinnedMeshRenderer child in model.GetComponentsInChildren<SkinnedMeshRenderer> ()) {
			child.gameObject.SetActive (false);
		}

		PrefabReference pr = model.GetComponent<PrefabReference> ();
		body = pr.body;
		clothes = pr.clothes;
		hair = pr.hair;
		bodyTex = pr.bodyTex;
		clothesTex = pr.clothesTex;
		hairTex = pr.hairTex;
		rootBone = pr.rootBone;

		body [EYEBROW].sharedMaterial.SetTexture ("_MainTex", bodyTex [TEX_EYEBROW]);
		hair [EAR].sharedMaterial.SetTexture ("_MainTex", hairTex [TEX_EAR]);
		hair [HAIR_B].sharedMaterial.SetTexture ("_MainTex", hairTex [TEX_HAIR_B]);
		hair [HAIR_F].sharedMaterial.SetTexture ("_MainTex", hairTex [TEX_HAIR_F]);
		hair [HAIR_S].sharedMaterial.SetTexture ("_MainTex", hairTex [TEX_HAIR_S]);
		hair [TAIL].sharedMaterial.SetTexture ("_MainTex", hairTex [TEX_TAIL]);

		foreach (SkinnedMeshRenderer b in body)
			b.gameObject.SetActive (true);
		foreach (SkinnedMeshRenderer c in clothes)
			c.gameObject.SetActive (true);
		foreach (SkinnedMeshRenderer h in hair)
			h.gameObject.SetActive (true);
	}

	void OnDisable() {

		if (!isReset) ResetColor ();
		DestroyImmediate (model);
	}

	void OnGUI() {

		_scrollPosition = EditorGUILayout.BeginScrollView (_scrollPosition); 

		EditorGUI.BeginChangeCheck ();

		using (new GUILayout.VerticalScope (EditorStyles.helpBox))
		{
			GUILayout.Label ("衣服");
		}
		e_clothes = (衣服)EditorGUILayout.EnumPopup ("衣服", e_clothes);
		EditorGUILayout.Space ();
		using (new GUILayout.VerticalScope (EditorStyles.helpBox))
		{
			GUILayout.Label ("顔");
		}
		e_eyebrow = (眉)EditorGUILayout.EnumPopup ("眉", e_eyebrow);
		e_face = (顔)EditorGUILayout.EnumPopup ("顔", e_face);
		e_mouth_def = (口)EditorGUILayout.EnumPopup ("デフォルト口", e_mouth_def);
		EditorGUILayout.Space ();
		using (new GUILayout.VerticalScope (EditorStyles.helpBox))
		{
			GUILayout.Label ("髪型");
		}
		e_hair_f = (前髪)EditorGUILayout.EnumPopup ("前髪", e_hair_f);
		e_hair_b = (後髪)EditorGUILayout.EnumPopup ("後髪", e_hair_b);
		e_hair_s = (横髪)EditorGUILayout.EnumPopup ("横髪", e_hair_s);
		EditorGUILayout.Space ();
		using (new GUILayout.VerticalScope (EditorStyles.helpBox)) {
			GUILayout.Label ("ぷちけも");
		}
		e_ear = (耳)EditorGUILayout.EnumPopup ("耳", e_ear);
		e_tail = (尻尾)EditorGUILayout.EnumPopup ("尻尾", e_tail);
		EditorGUILayout.Space ();
		using (new GUILayout.VerticalScope (EditorStyles.helpBox)) {
			GUILayout.Label ("色");
		}
		using (new GUILayout.HorizontalScope ()) {
			e_eye = (目色)EditorGUILayout.EnumPopup ("目の色", e_eye, GUILayout.Width(300));
			eyeColor = EditorGUILayout.ColorField (eyeColor);
		}
		if (!eachColorSetting) {
			EditorGUILayout.Space ();
			using (new GUILayout.HorizontalScope ()) {
				e_hair_color = (髪色)EditorGUILayout.EnumPopup ("毛色", e_hair_color, GUILayout.Width(300));
				hairColor = EditorGUILayout.ColorField (hairColor);
			}
		} else {
			using (new GUILayout.HorizontalScope ()) {
				e_eyebrow_color = (髪色)EditorGUILayout.EnumPopup ("眉色", e_eyebrow_color, GUILayout.Width(300));
				c_eyebrow_color = EditorGUILayout.ColorField (c_eyebrow_color);
			}
			EditorGUILayout.Space ();
			using (new GUILayout.HorizontalScope ()) {
				e_hair_b_color = (髪色)EditorGUILayout.EnumPopup ("後髪色", e_hair_b_color, GUILayout.Width(300));
				c_hair_b_color = EditorGUILayout.ColorField (c_hair_b_color);
			}
			using (new GUILayout.HorizontalScope ()) {
				e_hair_f_color = (髪色)EditorGUILayout.EnumPopup ("前髪色", e_hair_f_color, GUILayout.Width(300));
				c_hair_f_color = EditorGUILayout.ColorField (c_hair_f_color);
			}
			using (new GUILayout.HorizontalScope ()) {
				e_hair_s_color = (髪色)EditorGUILayout.EnumPopup ("横髪色", e_hair_s_color, GUILayout.Width(300));
				c_hair_s_color = EditorGUILayout.ColorField (c_hair_s_color);
			}
			EditorGUILayout.Space ();
			using (new GUILayout.HorizontalScope ()) {
				e_ear_color = (髪色)EditorGUILayout.EnumPopup ("けもみみ色", e_ear_color, GUILayout.Width(300));
				c_ear_color = EditorGUILayout.ColorField (c_ear_color);
			}
			using (new GUILayout.HorizontalScope ()) {
				e_tail_color = (髪色)EditorGUILayout.EnumPopup ("尻尾色", e_tail_color, GUILayout.Width(300));
				c_tail_color = EditorGUILayout.ColorField (c_tail_color);
			}
		}
		EditorGUILayout.Space ();
		clothesColor = EditorGUILayout.ColorField ("衣服色補正", clothesColor);

		//スクロールここまで
		EditorGUILayout.EndScrollView ();


		randomizePKM = EditorGUILayout.Toggle ("ランダムぷちけも生成", randomizePKM);
		EditorGUILayout.Space ();
		eachColorSetting = EditorGUILayout.Toggle ("毛色の個別設定", eachColorSetting);
		EditorGUILayout.Space ();


		//変更点アップデート
		if (EditorGUI.EndChangeCheck()) {
			
			clothes [CLOTHES].gameObject.SetActive (false);
			body [EYEBROW].gameObject.SetActive (false);
			body [FACE].gameObject.SetActive (false);
			body [MOUTH_DEF].gameObject.SetActive (false);
			hair [EAR].gameObject.SetActive (false);
			hair [HAIR_B].gameObject.SetActive (false);
			hair [HAIR_F].gameObject.SetActive (false);
			hair [HAIR_S].gameObject.SetActive (false);
			hair [TAIL].gameObject.SetActive (false);
			hair [EAR_C].gameObject.SetActive (false);
			disableClothes = false;

			ResetColor ();

			//ランダムぷちけも生成
			if (randomizePKM) {
				randomizePKM = false;
				if (EditorUtility.DisplayDialog ("ぷちけもメーカー", "プチけもみみ娘をランダム生成します\nよろしいですか？\n(現在の状況は失われます)", "OK")) {
					eachColorSetting = false;
					e_clothes = (衣服)Enum.ToObject (typeof(衣服), UnityEngine.Random.Range (0, Enum.GetNames (typeof(衣服)).Length));
					e_eyebrow = (眉)Enum.ToObject (typeof(眉), UnityEngine.Random.Range (0, Enum.GetNames (typeof(眉)).Length));
					e_face = (顔)Enum.ToObject (typeof(顔), UnityEngine.Random.Range (0, Enum.GetNames (typeof(顔)).Length));
					e_ear = (耳)Enum.ToObject (typeof(耳), UnityEngine.Random.Range (0, Enum.GetNames (typeof(耳)).Length));
					e_mouth_def = (口)Enum.ToObject (typeof(口), UnityEngine.Random.Range (0, Enum.GetNames (typeof(口)).Length));
					e_hair_b = (後髪)Enum.ToObject (typeof(後髪), UnityEngine.Random.Range (0, Enum.GetNames (typeof(後髪)).Length));
					e_hair_f = (前髪)Enum.ToObject (typeof(前髪), UnityEngine.Random.Range (0, Enum.GetNames (typeof(前髪)).Length));
					e_hair_s = (横髪)Enum.ToObject (typeof(横髪), UnityEngine.Random.Range (0, Enum.GetNames (typeof(横髪)).Length));
					e_tail = (尻尾)Enum.ToObject (typeof(尻尾), UnityEngine.Random.Range (0, Enum.GetNames (typeof(尻尾)).Length));
					e_eye = (目色)Enum.ToObject (typeof(目色), UnityEngine.Random.Range (0, Enum.GetNames (typeof(目色)).Length));
					e_hair_color = (髪色)Enum.ToObject (typeof(髪色), UnityEngine.Random.Range (0, Enum.GetNames (typeof(髪色)).Length));
				}
			}

			//毛色一括モード
			if (!eachColorSetting) {
				e_eyebrow_color = e_hair_color;
				e_ear_color = e_hair_color;
				e_hair_b_color = e_hair_color;
				e_hair_f_color = e_hair_color;
				e_hair_s_color = e_hair_color;
				e_tail_color = e_hair_color;
				c_eyebrow_color = hairColor;
				c_ear_color = hairColor;
				c_hair_b_color = hairColor;
				c_hair_f_color = hairColor;
				c_hair_s_color = hairColor;
				c_tail_color = hairColor;
			}

			//後頭部色
			body [FACE_BACK].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/face_back_" +  ((int)e_hair_b_color + 1).ToString() + ".mat");
			bodyTex [TEX_FACE_BACK] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/face_back/face_back_" +  ((int)e_hair_b_color + 1).ToString() + ".png");

			switch (e_clothes) {
			case 衣服.巫女服:
				clothes [CLOTHES] = model.transform.Find (" clothes_1").GetComponent<SkinnedMeshRenderer> ();
				body[BODY].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/clothes_1.mat");
				clothesTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "clothes/clothes_1.png");
				bodyTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/clothes/clothes_1.png");
				break;
			case 衣服.ゴスロリ服:
				clothes [CLOTHES] = model.transform.Find (" clothes_2").GetComponent<SkinnedMeshRenderer>();
				body[BODY].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/clothes_2.mat");
				clothesTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "clothes/clothes_2.png");
				bodyTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/clothes/clothes_2.png");
				break;
			case 衣服.アイドル服:
				clothes [CLOTHES] = model.transform.Find (" clothes_3").GetComponent<SkinnedMeshRenderer>();
				body[BODY].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/clothes_3.mat");
				clothesTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "clothes/clothes_3.png");
				bodyTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/clothes/clothes_3.png");
				break;
			case 衣服.厚着:
				clothes [CLOTHES] = model.transform.Find (" clothes_4").GetComponent<SkinnedMeshRenderer>();
				body[BODY].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/clothes_4.mat");
				clothesTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "clothes/clothes_4.png");
				bodyTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/clothes/clothes_4.png");
				break;
			case 衣服.セーラー服:
				clothes [CLOTHES] = model.transform.Find (" clothes_5").GetComponent<SkinnedMeshRenderer>();
				body[BODY].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/clothes_5.mat");
				clothesTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "clothes/clothes_5.png");
				bodyTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/clothes/clothes_5.png");
				break;
			case 衣服.メイド服:
				clothes [CLOTHES] = model.transform.Find (" clothes_6").GetComponent<SkinnedMeshRenderer>();
				body[BODY].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/clothes_6.mat");
				clothesTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "clothes/clothes_6.png");
				bodyTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/clothes/clothes_6.png");
				break;
			case 衣服.水着:
				body [BODY].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/clothes_0.mat");
				bodyTex [TEX_CLOTHES] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/clothes/clothes_0.png");
				disableClothes = true;
				break;
			}
			switch (e_eyebrow) {
			case 眉.眉1:
				body [EYEBROW] = model.transform.Find (" eyebrow_1").GetComponent<SkinnedMeshRenderer> ();
				bodyTex [TEX_EYEBROW] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/eyebrow/eyebrow_" + ((int)e_eyebrow_color + 1).ToString() + ".png");
				break;
			case 眉.眉2:
				body[EYEBROW] = model.transform.Find (" eyebrow_2").GetComponent<SkinnedMeshRenderer>();
				bodyTex [TEX_EYEBROW] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/eyebrow/eyebrow_" + ((int)e_eyebrow_color + 1).ToString() + ".png");
				break;
			case 眉.眉3:
				body[EYEBROW] = model.transform.Find (" eyebrow_3").GetComponent<SkinnedMeshRenderer>();
				bodyTex [TEX_EYEBROW] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/eyebrow/eyebrow_" + ((int)e_eyebrow_color + 1).ToString() + ".png");
				break;
			case 眉.眉4:
				body[EYEBROW] = model.transform.Find (" eyebrow_4").GetComponent<SkinnedMeshRenderer>();
				bodyTex [TEX_EYEBROW] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/eyebrow/eyebrow_" + ((int)e_eyebrow_color + 1).ToString() + ".png");
				break;
			case 眉.眉5:
				body[EYEBROW] = model.transform.Find (" eyebrow_5").GetComponent<SkinnedMeshRenderer>();
				bodyTex [TEX_EYEBROW] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/eyebrow/eyebrow_" + ((int)e_eyebrow_color + 1).ToString() + ".png");
				break;
			}
			switch (e_face) {
			case 顔.普通顔:
				body[FACE] = model.transform.Find (" face_1").GetComponent<SkinnedMeshRenderer>();
				bodyTex [TEX_FACE] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/face/face_1.png");
				break;
			case 顔.ジト目顔:
				body[FACE] = model.transform.Find (" face_2").GetComponent<SkinnedMeshRenderer>();
				bodyTex [TEX_FACE] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/face/face_2.png");
				break;
			case 顔.たれ目顔:
				body[FACE] = model.transform.Find (" face_3").GetComponent<SkinnedMeshRenderer>();
				bodyTex [TEX_FACE] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/face/face_3.png");
				break;
			}
			switch (e_mouth_def) {
			case 口.普通:
				body[MOUTH_DEF] = model.transform.Find (" mouth_def_1").GetComponent<SkinnedMeshRenderer>(); break;
			case 口.にっこり:
				body[MOUTH_DEF] = model.transform.Find (" mouth_def_2").GetComponent<SkinnedMeshRenderer>(); break;
			case 口.ω:
				body[MOUTH_DEF] = model.transform.Find (" mouth_def_3").GetComponent<SkinnedMeshRenderer>(); break;
			}
			switch (e_ear) {
			case 耳.猫耳:
				hair[EAR] = model.transform.Find (" ear_cat").GetComponent<SkinnedMeshRenderer>();
				hair[EAR_C] = model.transform.Find (" ear_cat_c").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_EAR] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/ear/" + ((int)e_ear_color + 1).ToString() + "/ear_cat.png");
				hairTex [TEX_EAR_C] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/ear/ear_cat_c.png");
				break;
			case 耳.狐耳:
				hair[EAR] = model.transform.Find (" ear_fox").GetComponent<SkinnedMeshRenderer>();
				hair[EAR_C] = model.transform.Find (" ear_fox_c").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_EAR] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/ear/" + ((int)e_ear_color + 1).ToString() + "/ear_fox.png");
				hairTex [TEX_EAR_C] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/ear/ear_fox_c.png");
				break;
			case 耳.兎耳:
				hair[EAR] = model.transform.Find (" ear_rabbit").GetComponent<SkinnedMeshRenderer>();
				hair[EAR_C] = model.transform.Find (" ear_rabbit_c").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_EAR] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/ear/" + ((int)e_ear_color + 1).ToString() + "/ear_rabbit.png");
				hairTex [TEX_EAR_C] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/ear/ear_rabbit_c.png");
				break;
			}
			switch (e_hair_b) {
			case 後髪.長髪:
				hair[HAIR_B] = model.transform.Find (" hair_b_1").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_B] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_b/" + ((int)e_hair_b_color + 1).ToString() + "/hair_b_1.png");
				break;
			case 後髪.短髪:
				hair[HAIR_B] = model.transform.Find (" hair_b_2").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_B] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_b/" + ((int)e_hair_b_color + 1).ToString() + "/hair_b_2.png");
				break;
			case 後髪.ツインロール:
				hair[HAIR_B] = model.transform.Find (" hair_b_3").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_B] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_b/" + ((int)e_hair_b_color + 1).ToString() + "/hair_b_3.png");
				break;
			case 後髪.おさげ1:
				hair[HAIR_B] = model.transform.Find (" hair_b_4").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_B] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_b/" + ((int)e_hair_b_color + 1).ToString() + "/hair_b_4.png");
				break;
			case 後髪.ツインテール:
				hair[HAIR_B] = model.transform.Find (" hair_b_5").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_B] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_b/" + ((int)e_hair_b_color + 1).ToString() + "/hair_b_5.png");
				break;
			case 後髪.おさげ2:
				hair[HAIR_B] = model.transform.Find (" hair_b_6").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_B] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_b/" + ((int)e_hair_b_color + 1).ToString() + "/hair_b_6.png");
				break;
			case 後髪.外ハネ:
				hair[HAIR_B] = model.transform.Find (" hair_b_7").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_B] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_b/" + ((int)e_hair_b_color + 1).ToString() + "/hair_b_7.png");
				break;
			case 後髪.ポニーテール:
				hair[HAIR_B] = model.transform.Find (" hair_b_8").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_B] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_b/" + ((int)e_hair_b_color + 1).ToString() + "/hair_b_8.png");
				break;
			}
			switch (e_hair_f) {
			case 前髪.普通1:
				hair[HAIR_F] = model.transform.Find (" hair_f_1").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_F] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_f/" + ((int)e_hair_f_color + 1).ToString() + "/hair_f_1.png");
				break;
			case 前髪.一本垂らし:
				hair[HAIR_F] = model.transform.Find (" hair_f_2").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_F] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_f/" + ((int)e_hair_f_color + 1).ToString() + "/hair_f_2.png");
				break;
			case 前髪.普通2:
				hair[HAIR_F] = model.transform.Find (" hair_f_3").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_F] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_f/" + ((int)e_hair_f_color + 1).ToString() + "/hair_f_3.png");
				break;
			case 前髪.短い:
				hair[HAIR_F] = model.transform.Find (" hair_f_4").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_F] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_f/" + ((int)e_hair_f_color + 1).ToString() + "/hair_f_4.png");
				break;
			case 前髪.目隠れ:
				hair[HAIR_F] = model.transform.Find (" hair_f_5").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_F] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_f/" + ((int)e_hair_f_color + 1).ToString() + "/hair_f_5.png");
				break;
			case 前髪.目隠れ2:
				hair[HAIR_F] = model.transform.Find (" hair_f_6").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_F] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_f/" + ((int)e_hair_f_color + 1).ToString() + "/hair_f_6.png");
				break;
			case 前髪.左右分け:
				hair[HAIR_F] = model.transform.Find (" hair_f_7").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_F] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_f/" + ((int)e_hair_f_color + 1).ToString() + "/hair_f_7.png");
				break;
			}
			switch (e_hair_s) {
			case 横髪.普通:
				hair[HAIR_S] = model.transform.Find (" hair_s_1").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_S] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_s/" + ((int)e_hair_s_color + 1).ToString() + "/hair_s_1.png");
				break;
			case 横髪.長い:
				hair[HAIR_S] = model.transform.Find (" hair_s_2").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_S] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_s/" + ((int)e_hair_s_color + 1).ToString() + "/hair_s_2.png");
				break;
			case 横髪.短い:
				hair[HAIR_S] = model.transform.Find (" hair_s_3").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_S] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_s/" + ((int)e_hair_s_color + 1).ToString() + "/hair_s_3.png");
				break;
			case 横髪.ふっくら:
				hair[HAIR_S] = model.transform.Find (" hair_s_4").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_S] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_s/" + ((int)e_hair_s_color + 1).ToString() + "/hair_s_4.png");
				break;
			case 横髪.結び:
				hair[HAIR_S] = model.transform.Find (" hair_s_5").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_S] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_s/" + ((int)e_hair_s_color + 1).ToString() + "/hair_s_5.png");
				break;
			case 横髪.外ハネ:
				hair[HAIR_S] = model.transform.Find (" hair_s_6").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_HAIR_S] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/hair_s/" + ((int)e_hair_s_color + 1).ToString() + "/hair_s_6.png");
				break;
			}
			switch (e_tail) {
			case 尻尾.猫尻尾:
				hair[TAIL] = model.transform.Find (" tail_cat").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_TAIL] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/tail/" + ((int)e_tail_color + 1).ToString() + "/tail_cat.png");
				break;
			case 尻尾.狐尻尾:
				hair[TAIL] = model.transform.Find (" tail_fox").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_TAIL] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/tail/" + ((int)e_tail_color + 1).ToString() + "/tail_fox.png");
				break;
			case 尻尾.兎尻尾:
				hair[TAIL] = model.transform.Find (" tail_rabbit").GetComponent<SkinnedMeshRenderer>();
				hairTex [TEX_TAIL] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "hair/tail/" + ((int)e_tail_color + 1).ToString() + "/tail_rabbit.png");
				break;
			}
			switch (e_eye) {
			case 目色.青:
				body[EYE].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/eye_1.mat");
				bodyTex [TEX_EYE_COLOR] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/eye/eye_1.png");
				break;
			case 目色.黄:
				body[EYE].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/eye_2.mat");
				bodyTex [TEX_EYE_COLOR] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/eye/eye_2.png");
				break;
			case 目色.赤:
				body[EYE].sharedMaterial = AssetDatabase.LoadAssetAtPath<Material> (texturePath + "body/Materials/eye_3.mat");
				bodyTex [TEX_EYE_COLOR] = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath + "body/eye/eye_3.png");
				break;
			}

			//色テクスチャ変更
			body [EYEBROW].sharedMaterial.SetTexture ("_MainTex", bodyTex [TEX_EYEBROW]);
			hair [EAR].sharedMaterial.SetTexture ("_MainTex", hairTex [TEX_EAR]);
			hair [HAIR_B].sharedMaterial.SetTexture ("_MainTex", hairTex [TEX_HAIR_B]);
			hair [HAIR_F].sharedMaterial.SetTexture ("_MainTex", hairTex [TEX_HAIR_F]);
			hair [HAIR_S].sharedMaterial.SetTexture ("_MainTex", hairTex [TEX_HAIR_S]);
			hair [TAIL].sharedMaterial.SetTexture ("_MainTex", hairTex [TEX_TAIL]);


			//色補正適用
			SetColor();

			//アクティブ
			if (!disableClothes) clothes [CLOTHES].gameObject.SetActive (true);
			body [EYEBROW].gameObject.SetActive (true);
			body [FACE].gameObject.SetActive (true);
			body [MOUTH_DEF].gameObject.SetActive (true);
			hair [EAR].gameObject.SetActive (true);
			hair [HAIR_B].gameObject.SetActive (true);
			hair [HAIR_F].gameObject.SetActive (true);
			hair [HAIR_S].gameObject.SetActive (true);
			hair [TAIL].gameObject.SetActive (true);
			hair [EAR_C].gameObject.SetActive (true);
		}
			


		if (GUILayout.Button ("エクスポート", GUI.skin.button, GUILayout.Height(30))) {
			
			savePath = EditorUtility.SaveFolderPanel ("エクスポート", "Assets", "");
			if (!String.IsNullOrEmpty (savePath)) {
				String[] transPath = Regex.Split(savePath, "/Assets/");
				savePath = "Assets/" + transPath [1];
				AssetDatabase.Refresh ();

				CreatePrefab ();
			}
		}
		EditorGUILayout.Space ();
	}

	void CreatePrefab() {

		//生成オブジェクト
		GameObject newModel;
		SkinnedMeshRenderer newRenderer;
		Mesh tmpMesh;
		Texture2D tmpBodyTex;
		Texture2D tmpClothesTex;
		Texture2D tmpHairTex;
		Material bodyMat;
		Material clothesMat;
		Material hairMat;

		//最後に消す
		SkinnedMeshRenderer[] unused = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);

		int disC = 0;
		if (!disableClothes) disC = 1;
		SkinnedMeshRenderer[] meshes = new SkinnedMeshRenderer[body.Length + hair.Length + disC];
		body.CopyTo (meshes, 0);
		hair.CopyTo (meshes, body.Length);
		if (!disableClothes) clothes.CopyTo (meshes, body.Length + hair.Length);


		//---保存先フォルダ作成
		if (!Directory.Exists(savePath + "/Texture")) AssetDatabase.CreateFolder(savePath, "Texture");
		if (!Directory.Exists(savePath + "/Model")) AssetDatabase.CreateFolder(savePath, "Model");
		if (!Directory.Exists(savePath + "/Texture/Material")) AssetDatabase.CreateFolder(savePath + "/Texture", "Material");


		//---メッシュ結合
		CombineInstance[] bodyCombine = new CombineInstance[body.Length];
		CombineInstance[] hairCombine = new CombineInstance[hair.Length];
		CombineInstance[] clothesCombine = new CombineInstance[clothes.Length];
		CombineInstance[] combine = new CombineInstance[2 + disC];

		Mesh bodyMesh = Combine (body, bodyCombine);
		Mesh hairMesh = Combine (hair, hairCombine);
		Mesh clothesMesh = Combine (clothes, clothesCombine);

		//tmp
		combine [0].mesh = bodyMesh;
		combine [0].transform = model.transform.localToWorldMatrix;
		combine [1].mesh = hairMesh;
		combine [1].transform = model.transform.localToWorldMatrix;
		if (!disableClothes) {
			combine [2].mesh = clothesMesh;
			combine [2].transform = model.transform.localToWorldMatrix;
		}
		tmpMesh = new Mesh ();
		tmpMesh.CombineMeshes (combine, false);

		Debug.Log ("メッシュ結合終了. 頂点数:" + tmpMesh.vertexCount);


		//---ブレンドシェイプ割り当て
		int vOffset = 0;

		for (int iMesh = 0; iMesh < meshes.Length; iMesh++) {

			Quaternion rot = Quaternion.Euler (meshes [iMesh].transform.rotation.eulerAngles);
			Vector3 scale = meshes [iMesh].transform.localScale;

			for (int shape = 0; shape < meshes[iMesh].sharedMesh.blendShapeCount; shape++) {
				for (int frame = 0; frame < meshes[iMesh].sharedMesh.GetBlendShapeFrameCount (shape); frame++) {

					Vector3[] vertices = new Vector3[meshes[iMesh].sharedMesh.vertexCount];
					Vector3[] normals  = new Vector3[meshes[iMesh].sharedMesh.vertexCount];
					Vector3[] tangents = new Vector3[meshes[iMesh].sharedMesh.vertexCount];
					Vector3[] tmpV = new Vector3[tmpMesh.vertexCount];
					Vector3[] tmpN = new Vector3[tmpMesh.vertexCount];
					Vector3[] tmpT = new Vector3[tmpMesh.vertexCount];

					string shapeName = meshes[iMesh].sharedMesh.GetBlendShapeName (shape);
					float frameWeight = meshes[iMesh].sharedMesh.GetBlendShapeFrameWeight (shape, frame);

					meshes [iMesh].sharedMesh.GetBlendShapeFrameVertices (shape, frame, vertices, normals, tangents);
					for (int i = 0; i < meshes[iMesh].sharedMesh.vertexCount; i++) {
						tmpV [vOffset + i] = vertices [i];
						tmpN [vOffset + i] = normals [i];
						tmpT [vOffset + i] = tangents [i];
						if (rot != Quaternion.identity) {
							tmpV [vOffset + i] = rot * tmpV [vOffset + i];
							tmpN [vOffset + i] = rot * tmpN [vOffset + i];
							tmpT [vOffset + i] = rot * tmpT [vOffset + i];
						}
						if (scale != new Vector3 (1, 1, 1)) {
							tmpV [vOffset + i] *= scale.x;
							tmpN [vOffset + i] *= scale.y;
							tmpT [vOffset + i] *= scale.z;
						}
					}
						
					if (tmpMesh.GetBlendShapeIndex(shapeName) == -1) {
						tmpMesh.AddBlendShapeFrame (shapeName, frameWeight, tmpV, tmpN, tmpT);
					}
				}
			}

			vOffset += meshes[iMesh].sharedMesh.vertexCount;
		}

		//メッシュ保存
		AssetDatabase.CreateAsset(tmpMesh, savePath + "/Model/Mesh.asset");
		AssetDatabase.SaveAssets ();

		//---テクスチャ合成
		tmpBodyTex = TextureCombine(bodyTex, 0);
		tmpHairTex = TextureCombine(hairTex, 1);
		//テクスチャ保存
		var bodyPng = tmpBodyTex.EncodeToPNG();
		var hairPng = tmpHairTex.EncodeToPNG();
		File.WriteAllBytes (savePath + "/Texture/Body.png", bodyPng);
		File.WriteAllBytes (savePath + "/Texture/Hair.png", hairPng);
		AssetDatabase.ImportAsset (savePath + "/Texture/Body.png");
		AssetDatabase.ImportAsset (savePath + "/Texture/Hair.png");
		if (!disableClothes) {
			tmpClothesTex = TextureCombine(clothesTex, 2);
			var clothesPng = tmpClothesTex.EncodeToPNG();
			File.WriteAllBytes (savePath + "/Texture/Clothes.png", clothesPng);
			AssetDatabase.ImportAsset (savePath + "/Texture/Clothes.png");
		}

		//---マテリアル設定
		bodyMat = new Material(Shader.Find(shader));
		bodyMat.SetTexture ("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(savePath + "/Texture/Body.png"));
		bodyMat.SetInt ("_CullMode", 0);
		hairMat = new Material (Shader.Find(shader));
		hairMat.SetTexture ("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(savePath + "/Texture/Hair.png"));
		hairMat.SetInt ("_CullMode", 0);
		//マテリアル保存
		AssetDatabase.CreateAsset(bodyMat, savePath + "/Texture/Material/Body.mat");
		AssetDatabase.CreateAsset(hairMat, savePath + "/Texture/Material/Hair.mat");
		if (!disableClothes) {
			clothesMat = new Material (Shader.Find(shader));
			clothesMat.SetTexture ("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(savePath + "/Texture/Clothes.png"));
			clothesMat.SetInt ("_CullMode", 0);
			AssetDatabase.CreateAsset(clothesMat, savePath + "/Texture/Material/Clothes.mat");
		}
		AssetDatabase.SaveAssets ();

		Material[] materials;
		if (!disableClothes) {
			materials = new Material[] {
				AssetDatabase.LoadAssetAtPath<Material> (savePath + "/Texture/Material/Body.mat"),
				AssetDatabase.LoadAssetAtPath<Material> (savePath + "/Texture/Material/Hair.mat"),
				AssetDatabase.LoadAssetAtPath<Material> (savePath + "/Texture/Material/Clothes.mat")
			};
		} else {
			materials = new Material[] {
				AssetDatabase.LoadAssetAtPath<Material> (savePath + "/Texture/Material/Body.mat"),
				AssetDatabase.LoadAssetAtPath<Material> (savePath + "/Texture/Material/Hair.mat")
			};
		}

		//---オブジェクト作成
		newModel = new GameObject();
		newModel.transform.SetParent (model.transform);
		newModel.name = "body";
		newRenderer = newModel.AddComponent <SkinnedMeshRenderer> ();
		newRenderer.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savePath + "/Model/Mesh.asset");
		newRenderer.materials = materials;


		//---ボーン設定
		newRenderer.rootBone = rootBone;
		newRenderer.bones = meshes [0].bones;

		//バインドポーズ
		Matrix4x4[] bps = new Matrix4x4[newRenderer.bones.Length];
		for(int cnt = 0; cnt < newRenderer.bones.Length; cnt++){
			bps [cnt] = newRenderer.bones [cnt].worldToLocalMatrix * model.transform.localToWorldMatrix;
		}
		newRenderer.sharedMesh.bindposes = bps;

		//ボーンウェイト、uv
		BoneWeight[] bw = new BoneWeight[newRenderer.sharedMesh.vertexCount];
		bool[] usedBone = new bool[newRenderer.bones.Length];
		vOffset = 0;
		for (int mc = 0; mc < meshes.Length; mc++) {
			for (int vc = 0; vc < meshes [mc].sharedMesh.vertexCount; vc++) {
				bw[vc + vOffset] = meshes[mc].sharedMesh.boneWeights[vc];
				if (bw [vc + vOffset].weight0 > 0) usedBone[bw[vc + vOffset].boneIndex0] = true;
				if (bw [vc + vOffset].weight1 > 0) usedBone[bw[vc + vOffset].boneIndex1] = true;
				if (bw [vc + vOffset].weight2 > 0) usedBone[bw[vc + vOffset].boneIndex2] = true;
				if (bw [vc + vOffset].weight3 > 0) usedBone[bw[vc + vOffset].boneIndex3] = true;
				//uv
				if (meshes [mc].sharedMesh.uv [vc] != null)
					newRenderer.sharedMesh.uv [vc + vOffset] = meshes [mc].sharedMesh.uv [vc];
			}

			vOffset += meshes[mc].sharedMesh.vertexCount;
		}
		newRenderer.sharedMesh.boneWeights = bw;


		//未使用ボーン非アクティブ化
		for (int i = 0; i < newRenderer.bones.Length; i++) {
			if (!usedBone[i]) newRenderer.bones [i].gameObject.SetActive (false);
		}

		ResetColor ();
		isReset = true;

		//不要オブジェクト削除
		foreach (SkinnedMeshRenderer u in unused) {
			DestroyImmediate (u.gameObject);
		}
		DestroyImmediate (model.GetComponent<PrefabReference>());

		//生成
		PrefabUtility.CreatePrefab (savePath + "/NewPKM.prefab", model);

		DestroyImmediate (model);
		this.Close ();

		EditorUtility.DisplayDialog ("ぷちけもメーカー", "アセット出力完了\n" + savePath + "/", "OK");
	}


	Mesh Combine(SkinnedMeshRenderer[] smr, CombineInstance[] ci) {
		for (int i = 0; i < smr.Length; i++) {
			ci [i].mesh = smr [i].sharedMesh;
			ci [i].transform = smr [i].transform.localToWorldMatrix;
		}
		Mesh mesh = new Mesh ();
		mesh.CombineMeshes (ci);

		return mesh;
	}

	Texture2D TextureCombine(Texture2D[] tex, int texType) {
		Texture2D texture = new Texture2D(tex[0].width, tex[0].height, TextureFormat.ARGB32, false);

		for (int y = 0; y < texture.height; y++) {
			for (int x = 0; x < texture.width; x++) {
				for (int i = 0; i < tex.Length; i++) {
					
					Color gpc = tex [i].GetPixel (x, y);
					Color addColor = Color.white;

					//色補正
					if (gpc.a > 0) {
						if (texType == 0) {
							switch (i) {
							case TEX_EYE_COLOR:
								addColor = eyeColor;
								break;
							case TEX_EYEBROW:
							case TEX_FACE_BACK:
								addColor = hairColor;
								break;
							}
						} else if (texType == 1) {
							switch (i) {
							case TEX_EAR:
							case TEX_HAIR_B:
							case TEX_HAIR_F:
							case TEX_HAIR_S:
							case TEX_TAIL:
								addColor = hairColor;
								break;
							}
						} else if (texType == 2)
							addColor = clothesColor;
						Color pc = new Color (gpc.r * addColor.r, gpc.g * addColor.g, gpc.b * addColor.b, gpc.a);

						texture.SetPixel (x, y, pc);
					}
				}
			}
		}
		texture.Apply ();

		return texture;
	}

	void ResetColor() {
		clothes [CLOTHES].sharedMaterial.SetColor ("_Color", Color.white);
		hair [EAR].sharedMaterial.SetColor("_Color", Color.white);
		hair [HAIR_B].sharedMaterial.SetColor("_Color", Color.white);
		hair [HAIR_F].sharedMaterial.SetColor("_Color", Color.white);
		hair [HAIR_S].sharedMaterial.SetColor("_Color", Color.white);
		hair [TAIL].sharedMaterial.SetColor("_Color", Color.white);
		body [FACE_BACK].sharedMaterial.SetColor("_Color", Color.white);
		body [EYEBROW].sharedMaterial.SetColor("_Color", Color.white);
		body [EYE].sharedMaterial.SetColor("_Color", Color.white);
	}

	void SetColor() {
		clothes [CLOTHES].sharedMaterial.SetColor ("_Color", clothesColor);
		hair [EAR].sharedMaterial.SetColor("_Color", c_ear_color);
		hair [HAIR_B].sharedMaterial.SetColor("_Color", c_hair_b_color);
		hair [HAIR_F].sharedMaterial.SetColor("_Color", c_hair_f_color);
		hair [HAIR_S].sharedMaterial.SetColor("_Color", c_hair_s_color);
		hair [TAIL].sharedMaterial.SetColor("_Color", c_tail_color);
		body [FACE_BACK].sharedMaterial.SetColor("_Color", c_hair_b_color);
		body [EYEBROW].sharedMaterial.SetColor("_Color", c_eyebrow_color);
		body [EYE].sharedMaterial.SetColor("_Color", eyeColor);
	}
}
﻿using Live2D.Cubism.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AMG
{
    public class Live2DModelController : MonoBehaviour
    {
        public string ConnectionIP = "/";
        public string ConnectionMessage = "";
		public GameObject ConnectionLost;
		public SettingPanelController SettingPanelController = null;

		private Vector3 ModelOffset;
		private Vector3 ConnectionLostOffset;

		private Dictionary<string, string> Parameters = new Dictionary<string, string>();
		public Dictionary<string, ParametersClass> InitedParameters = new Dictionary<string, ParametersClass>();

		public string DisplayName;
		public string ModelPath;
		public ArrayList animationClips;
		public Animation Animation;

		//等XD的SDK更新
		public bool LostReset = false;
		public int LostResetAction = 0;
		//0无 1动作 2PNG队列
		public string LostResetMotion = "";

		void Start()
        {
			InitParameters();
			GetParameters();
			InitBreath();
		}

		public void Update()
		{
			try
			{
				if (SettingPanelController != null)
				{
					if (SettingPanelController.GetModelSelected() == this.GetComponent<CubismModel>().name)
					{
						ProcessPosition();
					}
				}
				//处理存入的数据
				if (ConnectionIP != "/")
				{
					if (Globle.WSClients.ContainsKey(ConnectionIP))
					{
						ConnectionMessage = ObjectCopier.Clone(Globle.WSClients[ConnectionIP].message);
						if (ConnectionMessage != "")
						{
							DoJsonPrase(ConnectionMessage);
						}
					}
				}
				//更新模型位置
				ProcessModelParameter();
				
			}
			catch (Exception err)
			{
				Globle.DataLog = Globle.DataLog + "模型发生错误 " + err.Message + " : " + err.StackTrace;
			}
		}

		public void FixedUpdate()
		{
		}

		public void InitParameters()
		{
			Parameters.Add("paramEyeLOpen", "eyeLOpen|ELO");
			Parameters.Add("paramEyeROpen", "eyeROpen|ERO");
			Parameters.Add("paramAngleX", "headYaw|AX");
			Parameters.Add("paramAngleY", "headPitch|AY");
			Parameters.Add("paramAngleZ", "headRoll|AZ");
			Parameters.Add("paramEyeBallX", "eyeX|EBX");
			Parameters.Add("paramEyeBallY", "eyeY|EBY");
			Parameters.Add("paramBrowLForm", "eyeBrowLForm|BLF");
			Parameters.Add("paramBrowRForm", "eyeBrowRForm|BRF");
			Parameters.Add("paramBrowAngleL", "eyeBrowAngleL|BAL");
			Parameters.Add("paramBrowAngleR", "eyeBrowAngleR|BAR");
			Parameters.Add("paramBrowLY", "eyeBrowYL|BLY");
			Parameters.Add("paramBrowRY", "eyeBrowYR|BRY");
			Parameters.Add("paramMouthOpenY", "mouthOpenY|MOY");
			Parameters.Add("paramMouthForm", "mouthForm|MF");
			Parameters.Add("paramBreath", "Breath|/");
		}

		public void ProcessPosition()
		{
			if (Input.GetKey(KeyCode.LeftAlt))
			{
				if (Input.GetMouseButtonDown(0))
				{
					OnMouseDown();
				}
				if (Input.GetMouseButton(0))
				{
					OnMouseDrag();
				}
				if (Input.GetKey(KeyCode.LeftArrow))
				{
					transform.Rotate(0, 0, -0.5f, Space.Self);
				}
				if (Input.GetKey(KeyCode.RightArrow))
				{
					transform.Rotate(0, 0, +0.5f, Space.Self);
				}
				var Scale = Input.GetAxis("Mouse ScrollWheel") * 60f;
				this.GetComponent<CubismModel>().gameObject.transform.localScale += new Vector3(Scale, Scale);
			}
			if (Input.GetKey(KeyCode.LeftControl))
			{
				if (Input.GetMouseButtonDown(0))
				{
					OnMouseDownConnectionLost();
				}
				if (Input.GetMouseButton(0))
				{
					OnMouseDragConnectionLost();
				}
				var Scale = Input.GetAxis("Mouse ScrollWheel") * 0.005f;
				ConnectionLost.gameObject.transform.localScale += new Vector3(Scale, Scale);
			}
		}

		public void ProcessModelParameter()
		{
			foreach (KeyValuePair<string, ParametersClass> kvp in InitedParameters)
			{
				if (kvp.Value.Parameter != null && kvp.Value.Name != "paramBreath")
				{
					setParameter((CubismParameter)kvp.Value.Parameter, kvp.Value.NowValue, kvp.Value.MinValue, kvp.Value.MaxValue, kvp.Value.MinSetValue, kvp.Value.MaxSetValue);
				}
			}
		}

		void OnMouseDown()
		{
			ModelOffset = Camera.main.WorldToScreenPoint(transform.position) - Input.mousePosition;    
		}

		void OnMouseDrag()
		{
			var cc = Camera.main.ScreenToWorldPoint(Input.mousePosition + ModelOffset);
			transform.position = new Vector3(cc.x, cc.y, transform.position.z);
		}

		void OnMouseDownConnectionLost()
		{
			ConnectionLostOffset = Camera.main.WorldToScreenPoint(ConnectionLost.transform.position) - Input.mousePosition;  
		}

		void OnMouseDragConnectionLost()
		{
			var cc = Camera.main.ScreenToWorldPoint(Input.mousePosition + ConnectionLostOffset);
			ConnectionLost.transform.position = new Vector3(cc.x, cc.y, ConnectionLost.transform.position.z);
		}

		public void ResetModel()
		{
			foreach (KeyValuePair<string, ParametersClass> kvp in InitedParameters)
			{
				if (kvp.Value.Parameter != null && kvp.Value.Name != "paramBreath")
				{
					var para = (CubismParameter)kvp.Value.Parameter;
					para.Value = 0;
				}
			}
		}

		public void InitBreath()
		{
			var breathController = this.gameObject.AddComponent<CubismBreathController>();
			if (InitedParameters.ContainsKey("paramBreath"))
			{
				breathController.enabled = true;
				var par = (CubismParameter)InitedParameters["paramBreath"].Parameter;
				par.gameObject.AddComponent<CubismBreathParameter>();
			}
		}

		public void DoJsonPrase(string input)
        {
			try
			{
				var jsonResult = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(input);
				if (jsonResult.ContainsKey("SDK"))
				{
					foreach (KeyValuePair<string, ParametersClass> kvp in InitedParameters)
					{
						if (jsonResult.ContainsKey(kvp.Value.P2PSDKName))
						{
							kvp.Value.NowValue = float.Parse(jsonResult[kvp.Value.P2PSDKName].ToString());
							kvp.Value.MinSetValue = float.Parse(jsonResult[kvp.Value.P2PSDKName + "Min"].ToString());
							kvp.Value.MaxSetValue = float.Parse(jsonResult[kvp.Value.P2PSDKName + "Max"].ToString());
						}
					}
				}
				else
				{
					foreach (KeyValuePair<string, ParametersClass> kvp in InitedParameters)
					{
						if (jsonResult.ContainsKey(kvp.Value.SDKName))
						{
							kvp.Value.NowValue = float.Parse(jsonResult[kvp.Value.SDKName].ToString());
						}
					}
				}
			}
			catch { }
        }

		public void GetParameters()
		{
			var jsonDataPath = Application.streamingAssetsPath + "/Parameters.json";
			JObject jsonParams = Live2DParametersController.getParametersJson(jsonDataPath);
			var model = this.GetComponent<CubismModel>();
			foreach (KeyValuePair<string, string> kvp in Parameters)
			{
				var splitA = kvp.Value.Split('|');
				var paraC = new ParametersClass();
				paraC.Name = kvp.Key;
				var para = Live2DParametersController.getParametersFromJson(kvp.Key, jsonParams, model);
				paraC.Parameter = para;
				if (para != null)
				{
					paraC.MinValue = para.MinimumValue;
					paraC.MinSetValue = para.MinimumValue;
					paraC.MaxValue = para.MaximumValue;
					paraC.MaxSetValue = para.MaximumValue;
				}
				paraC.SDKName = splitA[0];
				paraC.P2PSDKName = splitA[1];
				InitedParameters.Add(kvp.Key, paraC);
			}
		}

		public void setParameter(CubismParameter param, float value, float MinValue, float MaxValue, float MinSetValue, float MaxSetValue)
		{
			if (param != null)
			{
				var get = (MaxValue - MinValue) * (value - MinSetValue) / (MaxSetValue - MinSetValue) + MinValue;
				if (value <= MinSetValue)
				{
					get = MinValue;
				}
				else if(value >= MaxSetValue)
				{
					get = MaxValue;
				}
				var smooth = Mathf.SmoothStep(param.Value, get, 0.5f);
				param.Value = smooth;
			}
		}

		public Dictionary<string, string> GetModelSettings()
		{
			var returnDict = new Dictionary<string, string>();
			foreach (KeyValuePair<string, ParametersClass> kvp in InitedParameters)
			{
				if (kvp.Value.Parameter != null && kvp.Value.Name != "paramBreath")
				{
					returnDict.Add(kvp.Value.Name, kvp.Value.MinSetValue.ToString() + "|" + kvp.Value.MaxSetValue.ToString());
				}
			}
			return returnDict;
		}

		public void SetModelSettings(Dictionary<string, string> userDict)
		{
			foreach (KeyValuePair<string, ParametersClass> kvp in InitedParameters)
			{
				if (userDict.ContainsKey(kvp.Value.Name) && kvp.Value.Name != "paramBreath")
				{
					var text = userDict[kvp.Value.Name];
					var splitA = text.Split('|');
					kvp.Value.MinSetValue = float.Parse(splitA[0]);
					kvp.Value.MaxSetValue = float.Parse(splitA[1]);
				}
			}
		}

		public Dictionary<string, string> GetModelOtherSettings()
		{
			var returnDict = new Dictionary<string, string>();
			returnDict.Add("LostReset", LostReset.ToString());
			returnDict.Add("LostResetAction", LostResetAction.ToString());
			returnDict.Add("LostResetMotion", LostResetMotion);
			return returnDict;
		}

		public void SetModelOtherSettings(Dictionary<string, string> otherDict)
		{
			LostReset = bool.Parse(otherDict["LostReset"]);
			LostResetAction = int.Parse(otherDict["LostResetAction"]);
			LostResetMotion = otherDict["LostResetMotion"];
		}

		public Dictionary<string, string> GetModelLocationSettings()
		{
			var returnDict = new Dictionary<string, string>();
			returnDict.Add("transformXValue", transform.position.x.ToString());
			returnDict.Add("transformYValue", transform.position.y.ToString());
			returnDict.Add("transformSValue", transform.localScale.x.ToString());
			returnDict.Add("transformRValue", transform.localEulerAngles.z.ToString());
			returnDict.Add("ctransformXValue", ConnectionLost.transform.position.x.ToString());
			returnDict.Add("ctransformYValue", ConnectionLost.transform.position.y.ToString());
			returnDict.Add("ctransformSValue", ConnectionLost.transform.localScale.x.ToString());
			return returnDict;
		}

		public void SetModelLocationSettings(Dictionary<string, string> locationInfo)
		{
			transform.position = new Vector3(Convert.ToSingle(locationInfo["transformXValue"]), Convert.ToSingle(locationInfo["transformYValue"]), transform.position.z);
			transform.localScale = new Vector3(Convert.ToSingle(locationInfo["transformSValue"]), Convert.ToSingle(locationInfo["transformSValue"]));
			transform.localEulerAngles = new Vector3(0, 0, Convert.ToSingle(locationInfo["transformRValue"]));
			ConnectionLost.transform.position = new Vector3(Convert.ToSingle(locationInfo["ctransformXValue"]), Convert.ToSingle(locationInfo["ctransformYValue"]), ConnectionLost.transform.position.z);
			ConnectionLost.transform.localScale = new Vector3(Convert.ToSingle(locationInfo["ctransformSValue"]), Convert.ToSingle(locationInfo["ctransformSValue"]));
		}

	}
}
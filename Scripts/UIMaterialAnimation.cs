using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[Serializable]
public class UIMaterialAnimation : IUIAnimationBase
{
    [Serializable]
    public class MaterialParam
    {
        public string Name;
        public bool TexST;
        public ShaderPropertyType PropertyType;
        public Gradient Color;
        public AnimationCurve Curve0;
        public AnimationCurve Curve1;
        public AnimationCurve Curve2;
        public AnimationCurve Curve3;

        private string TexSTName;
        public void GenTexSTName()
        {
            if (TexST && PropertyType == ShaderPropertyType.Texture)
            {
                TexSTName = Name + "_ST";
            }
        }

        public string GetName()
        {
            if (TexST && PropertyType == ShaderPropertyType.Texture)
            {
                return TexSTName;
            }
            return Name;
        }
    }

    [Serializable]
    public class UIMaterialProperty
    {
        public Graphic UIComp;
        public Material Mat;
        public List<MaterialParam> Params;

        public void ResetMaterial()
        {
            if (null != UIComp && null != Mat)
            {
                UIComp.material = Mat;
            }
        }
    };

    [Serializable]
    public class RendererMaterialProperty
    {
        public Renderer RendererComp;
        public Material Mat;
        public List<MaterialParam> Params;

        public void ResetMaterial()
        {
            if (null != RendererComp && null != Mat)
            {
                RendererComp.material = Mat;
            }
        }
    };

    public List<UIMaterialProperty> MaterialProperties;
    public List<RendererMaterialProperty> RendererMaterialProperties;
    private static Dictionary<string, int> _propertyIds = new Dictionary<string, int>();
    private static void CachePropertyId(MaterialParam param)
    {
        string name = param.GetName();
        if (!_propertyIds.ContainsKey(name))
        {
            _propertyIds.Add(name, Shader.PropertyToID(name));
        }
    }

    public void Init(GameObject target)
    {
        if (null != MaterialProperties)
        {
            for (int i = 0; i < MaterialProperties.Count; ++i)
            {
                UIMaterialProperty property = MaterialProperties[i];
                if (null != property.UIComp && null != property.Mat)
                {
                    property.UIComp.material = UnityEngine.Object.Instantiate(property.Mat); // UI材质需要复制，因为返回的是共享材质
                }

                for (int j = 0; j < property.Params.Count; ++j)
                {
                    MaterialParam param = property.Params[j];
                    param.GenTexSTName();
                    CachePropertyId(param);
                }
            }
        }

        if (null != RendererMaterialProperties)
        {
            for (int i = 0; i < RendererMaterialProperties.Count; ++i)
            {
                RendererMaterialProperty property = RendererMaterialProperties[i];
                if (null != property.RendererComp && null != property.Mat)
                {
                    property.RendererComp.material = UnityEngine.Object.Instantiate(property.Mat); // 材质需要复制，因为返回的是共享材质
                }

                for (int j = 0; j < property.Params.Count; ++j)
                {
                    MaterialParam param = property.Params[j];
                    param.GenTexSTName();
                    CachePropertyId(param);
                }
            }
        }
    }

    private void UpdateRendererMaterialProperty(RendererMaterialProperty matProperty, float rate)
    {
        if (null != matProperty && null != matProperty.RendererComp)
        {
            Material mat = matProperty.RendererComp.sharedMaterial;
            if (Application.isPlaying)
            {
                mat = matProperty.RendererComp.material;
            }

            if (null == mat) return;

            for (int j = 0; j < matProperty.Params.Count; ++j)
            {
                MaterialParam param = matProperty.Params[j];
                if (!_propertyIds.ContainsKey(param.GetName()))
                {
                    _propertyIds.Add(param.GetName(), Shader.PropertyToID(param.GetName()));
                }

                if (ShaderPropertyType.Color == param.PropertyType)
                {
                    mat.SetColor(_propertyIds[param.GetName()], param.Color.Evaluate(rate));
                }
                else if (ShaderPropertyType.Float == param.PropertyType
                    || ShaderPropertyType.Range == param.PropertyType)
                {
                    mat.SetFloat(_propertyIds[param.GetName()], param.Curve0.Evaluate(rate));
                }
                else if ((ShaderPropertyType.Vector == param.PropertyType)
                    || (param.TexST && ShaderPropertyType.Texture == param.PropertyType))
                {
                    Vector4 vector = new Vector4(param.Curve0.Evaluate(rate)
                        , param.Curve1.Evaluate(rate)
                        , param.Curve2.Evaluate(rate)
                        , param.Curve3.Evaluate(rate));
                    mat.SetVector(_propertyIds[param.GetName()], vector);
                }
            }
        }
    }

    private void UpdateMaterialProperty(UIMaterialProperty matProperty, float rate)
    {
        if (null != matProperty && null != matProperty.UIComp && null != matProperty.UIComp.material)
        {
            Material mat = matProperty.UIComp.materialForRendering;
            int count = matProperty.Params.Count;
            for (int j = 0; j < count; ++j)
            {
                MaterialParam param = matProperty.Params[j];
                if (!_propertyIds.ContainsKey(param.GetName()))
                {
                    _propertyIds.Add(param.GetName(), Shader.PropertyToID(param.GetName()));
                }

                if (ShaderPropertyType.Color == param.PropertyType)
                {
                    mat.SetColor(_propertyIds[param.GetName()], param.Color.Evaluate(rate));
                }
                else if (ShaderPropertyType.Float == param.PropertyType
                    || ShaderPropertyType.Range == param.PropertyType)
                {
                    mat.SetFloat(_propertyIds[param.GetName()], param.Curve0.Evaluate(rate));
                }
                else if ((ShaderPropertyType.Vector == param.PropertyType)
                    || (param.TexST && ShaderPropertyType.Texture == param.PropertyType))
                {
                    Vector4 vector = new Vector4(param.Curve0.Evaluate(rate)
                        , param.Curve1.Evaluate(rate)
                        , param.Curve2.Evaluate(rate)
                        , param.Curve3.Evaluate(rate));
                    mat.SetVector(_propertyIds[param.GetName()], vector);
                }
            }
        }
    }

    public void OnUpdate(float rate)
    {
        if (null != MaterialProperties)
        {
            for (int i = 0; i < MaterialProperties.Count; ++i)
            {
                UpdateMaterialProperty(MaterialProperties[i], rate);
            }
        }
        if (null != RendererMaterialProperties)
        {
            for (int i = 0; i < RendererMaterialProperties.Count; ++i)
            {
                UpdateRendererMaterialProperty(RendererMaterialProperties[i], rate);
            }
        }
    }

    public void Release()
    {
        ResetMaterial();
    }

    public void OnStart()
    {
    }

    public void OnStop()
    {
    }

    public void ResetMaterial()
    {
        if (null != MaterialProperties)
        {
            for (int i = 0; i < MaterialProperties.Count; ++i)
            {
                MaterialProperties[i].ResetMaterial();
            }
        }
        if (null != RendererMaterialProperties)
        {
            for (int i = 0; i < RendererMaterialProperties.Count; ++i)
            {
                RendererMaterialProperties[i].ResetMaterial();
            }
        }
    }
}

﻿//*********************************************************************
//xCAD
//Copyright(C) 2022 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xarial.XCad.Annotations;
using Xarial.XCad.Documents;
using Xarial.XCad.Exceptions;
using Xarial.XCad.Features;
using Xarial.XCad.Features.CustomFeature;
using Xarial.XCad.Features.CustomFeature.Attributes;
using Xarial.XCad.Features.CustomFeature.Enums;
using Xarial.XCad.Features.CustomFeature.Services;
using Xarial.XCad.Geometry;
using Xarial.XCad.Reflection;
using Xarial.XCad.Toolkit.Utils;
using Xarial.XCad.Utils.Reflection;

namespace Xarial.XCad.Utils.CustomFeature
{
    public class CustomFeatureParameter
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public object Value { get; set; }
    }

    public abstract class CustomFeatureParametersParser
    {
        private class DimensionParamData
        {
            internal CustomFeatureDimensionType_e Type { get; private set; }
            internal double Value { get; private set; }

            internal DimensionParamData(CustomFeatureDimensionType_e type, double val)
            {
                Type = type;
                Value = val;
            }
        }

        private class PropertyObject<TObject>
        {
            internal TObject Object { get; private set; }
            internal string PropertyName { get; private set; }

            internal PropertyObject(string prpName, TObject obj)
            {
                PropertyName = prpName;
                Object = obj;
            }
        }

        protected const string VERSION_DIMENSIONS_NAME = "__dimsVersion";
        protected const string VERSION_PARAMETERS_NAME = "__paramsVersion";

        private readonly FaultObjectFactory m_FaultObjectFactory;

        public CustomFeatureParametersParser() 
        {
            m_FaultObjectFactory = new FaultObjectFactory();
        }

        public virtual object GetParameters(IXCustomFeature feat, IXDocument model, Type paramsType,
                    out IXDimension[] dispDims, out string[] dispDimParams, out IXBody[] editBodies,
                    out IXSelObject[] sels, out CustomFeatureOutdateState_e state)
        {
            Dictionary<string, object> featRawParams;
            IXDimension[] featDims;
            IXSelObject[] featSels;
            IXBody[] featBodies;

            ExtractRawParameters(feat, model, out featRawParams, out featDims, out featSels, out featBodies);

            var parameters = new Dictionary<string, object>();

            var paramsVersion = new Version();
            var dimsVersion = new Version();

            if (featRawParams?.Any() == true)
            {
                foreach (var featRawParam in featRawParams)
                {
                    var paramName = featRawParam.Key;

                    var paramVal = featRawParam.Value;

                    if (paramName == VERSION_PARAMETERS_NAME)
                    {
                        paramsVersion = new Version(paramVal.ToString());
                    }
                    else if (paramName == VERSION_DIMENSIONS_NAME)
                    {
                        dimsVersion = new Version(paramVal.ToString());
                    }
                    else
                    {
                        parameters.Add(paramName, paramVal);
                    }
                }
            }

            ConvertParameters(paramsType, paramsVersion,
                conv => conv.Convert(model, feat, ref parameters, ref featSels, ref featDims, ref featBodies));

            var resParams = Activator.CreateInstance(paramsType);

            var dispDimParamsMap = new SortedDictionary<int, string>();

            TraverseParametersDefinition(resParams,
                (obj, prp) =>
                {
                    AssignObjectsToProperty(obj, featSels, prp, parameters);
                },
                (dimType, obj, prp) =>
                {
                    var dimIndices = GetObjectIndices(prp, parameters);

                    if (dimIndices.Length != 1)
                    {
                        throw new InvalidOperationException(
                            "It could only be one index associated with dimension");
                    }

                    var dimInd = dimIndices.First();

                    if (featDims.Length > dimInd)
                    {
                        var dispDim = featDims[dimInd];

                        var val = dispDim.Value;

                        if (!double.IsNaN(val))
                        {
                            prp.SetValue(obj, val, null);
                        }

                        dispDimParamsMap.Add(dimInd, prp.Name);
                    }
                    else
                    {
                        throw new IndexOutOfRangeException(
                            $"Dimension at index {dimInd} is not present in the macro feature");
                    }
                },
                (obj, prp) =>
                {
                    AssignObjectsToProperty(obj, featBodies, prp, parameters);
                },
                (obj, prp) =>
                {
                    if (TryGetParameterValue(parameters, prp.Name, out object paramVal))
                    {
                        object val = null;

                        if (paramVal != null)
                        {
                            if (prp.PropertyType.IsEnum)
                            {
                                val = Enum.Parse(prp.PropertyType, paramVal.ToString());
                            }
                            else
                            {
                                val = Convert.ChangeType(paramVal, prp.PropertyType);
                            }
                        }

                        prp.SetValue(obj, val, null);
                    }
                });

            dispDims = featDims;
            editBodies = featBodies;
            sels = featSels;

            state = GetState(feat, featDims);

            dispDimParams = dispDimParamsMap.Values.ToArray();

            return resParams;
        }

        public void Parse(object parameters,
                    out CustomFeatureParameter[] atts, out IXSelObject[] selection,
                    out CustomFeatureDimensionType_e[] dimTypes, out double[] dimValues, out IXBody[] editBodies)
        {
            if (parameters == null) 
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var paramAttsList = new List<CustomFeatureParameter>();

            var selectionList = new List<PropertyObject<IXSelObject>>();
            var dimsList = new List<PropertyObject<DimensionParamData>>();
            var editBodiesList = new List<PropertyObject<IXBody>>();

            TraverseParametersDefinition(parameters,
                (obj, prp) =>
                {
                    ReadObjectsValueFromProperty(obj, prp, selectionList);
                },
                (dimType, obj, prp) =>
                {
                    var val = Convert.ToDouble(prp.GetValue(obj, null));
                    dimsList.Add(new PropertyObject<DimensionParamData>(
                        prp.Name, new DimensionParamData(dimType, val)));
                },
                (obj, prp) =>
                {
                    ReadObjectsValueFromProperty(obj, prp, editBodiesList);
                },
                (obj, prp) =>
                {
                    var val = prp.GetValue(obj, null);

                    paramAttsList.Add(new CustomFeatureParameter()
                    {
                        Name = prp.Name,
                        Type = prp.PropertyType,
                        Value = val
                    });
                });

            parameters.GetType().TryGetAttribute<ParametersVersionAttribute>(a =>
            {
                var setVersionFunc = new Action<string, Version>((n, v) =>
                {
                    var versParamIndex = paramAttsList.FindIndex(l => l.Name == n);

                    if (versParamIndex == -1)
                    {
                        paramAttsList.Add(new CustomFeatureParameter()
                        {
                            Name = n,
                            Type = typeof(string),
                            Value = v.ToString()
                        });
                    }
                    else
                    {
                        paramAttsList[versParamIndex].Value = v.ToString();
                    }
                });

                setVersionFunc.Invoke(VERSION_PARAMETERS_NAME, a.Version);
                setVersionFunc.Invoke(VERSION_DIMENSIONS_NAME, a.Version);
            });

            selection = AddParametersForObjects(selectionList, paramAttsList);
            var dimParams = AddParametersForObjects(dimsList, paramAttsList);
            editBodies = AddParametersForObjects(editBodiesList, paramAttsList);

            atts = paramAttsList.ToArray();

            if (dimParams != null)
            {
                dimTypes = dimParams.Select(d => d.Type).ToArray();
                dimValues = dimParams.Select(d => d.Value).ToArray();
            }
            else
            {
                dimTypes = null;
                dimValues = null;
            }
        }

        public virtual void SetParameters(IXDocument model, IXCustomFeature feat,
                    object parameters, out CustomFeatureOutdateState_e state)
        {
            CustomFeatureParameter[] param;
            IXSelObject[] selection;
            double[] dimValues;
            IXBody[] bodies;

            Parse(parameters,
                out param,
                out selection, out _, out dimValues, out bodies);

            var dispDims = GetDimensions(feat);

            //var dimsVersion = GetDimensionsVersion(feat);

            //ConvertParameters(parameters.GetType(), dimsVersion, conv =>
            //{
            //    dispDims = conv.ConvertDisplayDimensions(model, feat, dispDims);
            //});

            if (dispDims != null)
            {
                if (dispDims.Length != dimValues.Length)
                {
                    throw new ParametersMismatchException("Dimensions mismatch");
                }
            }

            state = GetState(feat, dispDims);

            SetParametersToFeature(feat, selection, bodies, dispDims, dimValues, param);
        }
        
        //TODO: need to spearate to different methods
        protected abstract void ExtractRawParameters(IXCustomFeature feat, IXDocument doc,
            out Dictionary<string, object> parameters, out IXDimension[] dimensions,
            out IXSelObject[] selection, out IXBody[] editBodies);

        protected abstract IXDimension[] GetDimensions(IXCustomFeature feat);

        protected abstract CustomFeatureOutdateState_e GetState(IXCustomFeature featData, IXDimension[] dispDims);

        protected abstract Version GetVersion(IXFeature featData, string name);

        protected abstract void SetParametersToFeature(IXCustomFeature feat,
            IXSelObject[] selection, IXBody[] editBodies, IXDimension[] dims, double[] dimValues, CustomFeatureParameter[] param);

        private T[] AddParametersForObjects<T>(List<PropertyObject<T>> objects,
            List<CustomFeatureParameter> paramList)
            where T : class
        {
            T[] retVal = null;

            if (objects != null && objects.Any())
            {
                var allObjects = objects.Select(o => o.Object)
                    .Distinct()
                    .Where(o => o != null).ToList();

                var paramsGroup = objects.GroupBy(o => o.PropertyName).ToDictionary(g => g.Key,
                    g =>
                    {
                        return string.Join(",", g.Select(
                            e =>
                            {
                                var index = allObjects.IndexOf(e.Object);
                                return index;
                            }).ToArray());
                    });

                paramList.AddRange(paramsGroup.Select(g => new CustomFeatureParameter()
                {
                    Name = g.Key,
                    Value = g.Value,
                    Type = typeof(string)
                }));

                retVal = allObjects.ToArray();
            }

            return retVal;
        }

        private void AssignObjectsToProperty(object resParams, Array availableObjects,
            PropertyInfo prp, Dictionary<string, object> parameters)
        {
            var indices = GetObjectIndices(prp, parameters);

            if (indices != null && indices.Any())
            {
                if (availableObjects == null)
                {
                    availableObjects = new object[0];
                }

                object val = null;

                if (indices.All(i => availableObjects.Length > i))
                {
                    if (typeof(IList).IsAssignableFrom(prp.PropertyType))
                    {
                        //TODO: potential issues with IList as IEnumerable can come here as well and it will fail

                        var lst = (IList)prp.GetValue(resParams, null);
                        
                        if (lst != null)
                        {
                            lst.Clear();
                        }
                        else
                        {
                            lst = (IList)Activator.CreateInstance(prp.PropertyType);
                        }

                        val = lst;

                        if (indices.Length == 1 && indices.First() == -1)
                        {
                            val = null; //no entities in the list
                        }
                        else
                        {
                            foreach (var obj in indices.Select(i =>
                            {
                                object elem;

                                if (i == -1)
                                {
                                    elem = null;
                                }
                                else
                                {
                                    elem = availableObjects.GetValue(i);

                                    if (elem == null)
                                    {
                                        var elemType = prp.PropertyType.GetArgumentsOfGenericType(typeof(IList<>))[0];

                                        elem = m_FaultObjectFactory.CreateFaultObject(elemType);
                                    }
                                }

                                return elem;
                            }))
                            {
                                lst.Add(obj);
                            }
                        }
                    }
                    else
                    {
                        if (indices.Length > 1)
                        {
                            throw new InvalidOperationException($"Multiple selection indices at {prp.Name} could only be associated with the List");
                        }

                        var index = indices.First();

                        if (index == -1)
                        {
                            val = null;
                        }
                        else
                        {
                            val = availableObjects.GetValue(index);

                            if (val == null)
                            {
                                val = m_FaultObjectFactory.CreateFaultObject(prp.PropertyType);
                            }
                        }
                    }
                }
                else
                {
                    throw new IndexOutOfRangeException(
                        $"Some of the referenced entity indices ({string.Join(", ", indices)}) are out of range for '{prp.Name}' (max size: {availableObjects.Length})");
                }

                prp.SetValue(resParams, val, null);
            }
            else
            {
                throw new NullReferenceException($"Indices are not set for {prp.PropertyType.Name}");
            }
        }

        private void ConvertParameters(Type paramsType, Version paramVersion,
            Action<IParameterConverter> converter)
        {
            IParametersVersionConverter versConv = null;
            var curParamVersion = new Version();

            paramsType.TryGetAttribute<ParametersVersionAttribute>(a =>
            {
                versConv = a.VersionConverter;
                curParamVersion = a.Version;
            });

            if (curParamVersion != paramVersion)
            {
                if (curParamVersion > paramVersion)
                {
                    if (versConv != null)
                    {
                        if (versConv.ContainsKey(curParamVersion))
                        {
                            foreach (var conv in versConv.Where(
                                v => v.Key > paramVersion && v.Key <= curParamVersion)
                                .OrderBy(v => v.Key))
                            {
                                converter.Invoke(conv.Value);
                            }
                        }
                        else
                        {
                            throw new NullReferenceException($"{curParamVersion} version of parameters {paramsType.FullName} is not registered");
                        }
                    }
                    else
                    {
                        throw new NullReferenceException("Version converter is not set");
                    }
                }
                else
                {
                    throw new FutureVersionParametersException(paramsType, curParamVersion, paramVersion);
                }
            }
        }

        private Version GetDimensionsVersion(IXFeature featData)
            => GetVersion(featData, VERSION_DIMENSIONS_NAME);

        private int[] GetObjectIndices(PropertyInfo prp, Dictionary<string, object> parameters)
        {
            int[] indices = null;

            object indValues;

            if (parameters.TryGetValue(prp.Name, out indValues))
            {
                indices = indValues.ToString().Split(',').Select(i => int.Parse(i)).ToArray();
            }

            return indices;
        }

        private bool TryGetParameterValue(Dictionary<string, object> parameters, string name, out object value)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            return parameters.TryGetValue(name, out value);
        }

        private void ReadObjectsValueFromProperty<T>(object parameters,
                    PropertyInfo prp, List<PropertyObject<T>> list)
                    where T : class
        {
            var val = prp.GetValue(parameters, null);

            if (val is IList)
            {
                if ((val as IList).Count != 0)
                {
                    foreach (T lstElem in val as IList)
                    {
                        list.Add(new PropertyObject<T>(prp.Name, lstElem));
                    }
                }
                else
                {
                    list.Add(new PropertyObject<T>(prp.Name, null));
                }
            }
            else
            {
                list.Add(new PropertyObject<T>(prp.Name, val as T));
            }
        }

        //TODO: implement the support for the nested types
        public void TraverseParametersDefinition(object parameters,
                    Action<object, PropertyInfo> selParamHandler,
                    Action<CustomFeatureDimensionType_e, object, PropertyInfo> dimParamHandler,
                    Action<object, PropertyInfo> editBodyHandler,
                    Action<object, PropertyInfo> dataParamHandler)
        {
            var paramsType = parameters.GetType();

            foreach (var prp in paramsType.GetProperties())
            {
                if (prp.TryGetAttribute<ParameterExcludeAttribute>() == null)
                {
                    var prpType = prp.PropertyType;

                    var dimAtt = prp.TryGetAttribute<ParameterDimensionAttribute>();
                    var editBodyAtt = prp.TryGetAttribute<ParameterEditBodyAttribute>();

                    if (dimAtt != null)
                    {
                        var dimType = dimAtt.DimensionType;
                        dimParamHandler.Invoke(dimType, parameters, prp);
                    }
                    else if (editBodyAtt != null)
                    {
                        editBodyHandler.Invoke(parameters, prp);
                    }
                    else if (typeof(IXSelObject).IsAssignableFrom(prpType)
                        || typeof(IEnumerable<IXSelObject>).IsAssignableFrom(prpType))
                    {
                        selParamHandler.Invoke(parameters, prp);
                    }
                    else
                    {
                        if (typeof(IConvertible).IsAssignableFrom(prpType))
                        {
                            dataParamHandler.Invoke(parameters, prp);
                        }
                        else
                        {
                            throw new NotSupportedException(
                                $"{prp.Name} is not supported as the parameter of macro feature. Currently only types implementing IConvertible are supported (e.g. primitive types, string, DateTime, decimal)");
                        }
                    }
                }
            }
        }
    }
}
﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace HigLabo.Core
{
    /// <summary>
    /// Configure Object Mapping settings.
    /// </summary>
    public class ObjectMapConfig
    {
        private class ObjectMapConfigMethodAttribute : Attribute
        {
            public String Name { get; set; }
        }
        private class MapMethodInfo<TSource, TTarget>
        {
            public Func<ObjectMapConfig, TSource, TTarget, MappingContext, TTarget> Map { get; set; }
            public Func<ObjectMapConfig, TSource, TTarget, MappingContext, TTarget> MapChildObject { get; set; }
        }
        public static ObjectMapConfig Current { get; private set; }

        private static readonly String System_Collections_Generic_ICollection_1 = "System.Collections.Generic.ICollection`1";
        private static readonly String System_Collections_Generic_IEnumerable_1 = "System.Collections.Generic.IEnumerable`1";
        private readonly ConcurrentDictionary<ObjectMapTypeInfo, Object> _Methods = new ConcurrentDictionary<ObjectMapTypeInfo, Object>();

        private static readonly MethodInfo _MapMethod = null;
        private static readonly MethodInfo _MapToMethod = null;
        private static readonly MethodInfo _MapReferenceMethod = null;
        private static readonly MethodInfo _MappingContext_NullPropertyMapMode_GetMethod = null;
        private static readonly MethodInfo _MappingContext_CollectionElementMapMode_GetMethod = null;
        private static readonly MethodInfo _ObjectMapConfig_TypeConverterProperty_GetMethod = null;
        private static readonly ConcurrentDictionary<Type, MethodInfo> _TypeConverter_ToEnumMethods = new ConcurrentDictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> _TypeConverter_ToTypeMethods = new Dictionary<Type, MethodInfo>();

        private List<MapPostAction> _PostActions = new List<MapPostAction>();

        public List<PropertyMappingRule> PropertyMapRules { get; private set; }
        public List<DictionaryMappingRule> DictionaryMappingRules { get; private set; }
        public TypeConverter TypeConverter { get; set; }
        public Int32 MaxCallStack { get; set; }
        public StringComparer DictionaryKeyStringComparer { get; set; }
        public NullPropertyMapMode NullPropertyMapMode { get; set; }
        public CollectionElementMapMode CollectionElementMapMode { get; set; }

        static ObjectMapConfig()
        {
            Current = new ObjectMapConfig();
            _MapMethod = GetMethodInfo("Map");
            _MapToMethod = GetMethodInfo("MapTo");
            _MapReferenceMethod = GetMethodInfo("MapReference");
            _MappingContext_NullPropertyMapMode_GetMethod = typeof(MappingContext).GetProperty("NullPropertyMapMode", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
            _MappingContext_CollectionElementMapMode_GetMethod = typeof(MappingContext).GetProperty("CollectionElementMapMode", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
            _ObjectMapConfig_TypeConverterProperty_GetMethod = typeof(ObjectMapConfig).GetProperty("TypeConverter", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
            InitializeTypeConverter_ToTypeMethods();
        }
        private static void InitializeTypeConverter_ToTypeMethods()
        {
            var d = _TypeConverter_ToTypeMethods;
            d.Add(typeof(String), GetTypeConverterToTypeMethodInfo(typeof(String)));
            d.Add(typeof(Boolean), GetTypeConverterToTypeMethodInfo(typeof(Boolean)));
            d.Add(typeof(Guid), GetTypeConverterToTypeMethodInfo(typeof(Guid)));
            d.Add(typeof(SByte), GetTypeConverterToTypeMethodInfo(typeof(SByte)));
            d.Add(typeof(Int16), GetTypeConverterToTypeMethodInfo(typeof(Int16)));
            d.Add(typeof(Int32), GetTypeConverterToTypeMethodInfo(typeof(Int32)));
            d.Add(typeof(Int64), GetTypeConverterToTypeMethodInfo(typeof(Int64)));
            d.Add(typeof(Byte), GetTypeConverterToTypeMethodInfo(typeof(Byte)));
            d.Add(typeof(UInt16), GetTypeConverterToTypeMethodInfo(typeof(UInt16)));
            d.Add(typeof(UInt32), GetTypeConverterToTypeMethodInfo(typeof(UInt32)));
            d.Add(typeof(UInt64), GetTypeConverterToTypeMethodInfo(typeof(UInt64)));
            d.Add(typeof(Single), GetTypeConverterToTypeMethodInfo(typeof(Single)));
            d.Add(typeof(Double), GetTypeConverterToTypeMethodInfo(typeof(Double)));
            d.Add(typeof(Decimal), GetTypeConverterToTypeMethodInfo(typeof(Decimal)));
            d.Add(typeof(TimeSpan), GetTypeConverterToTypeMethodInfo(typeof(TimeSpan)));
            d.Add(typeof(DateTime), GetTypeConverterToTypeMethodInfo(typeof(DateTime)));
            d.Add(typeof(DateTimeOffset), GetTypeConverterToTypeMethodInfo(typeof(DateTimeOffset)));
            d.Add(typeof(Encoding), GetTypeConverterToTypeMethodInfo(typeof(Encoding)));

        }
        private static MethodInfo GetTypeConverterToTypeMethodInfo(Type type)
        {
            return typeof(TypeConverter).GetMethod("To" + type.Name, new Type[] { typeof(Object) });
        }

        public ObjectMapConfig()
        {
            this.TypeConverter = new TypeConverter();

            this.PropertyMapRules = new List<PropertyMappingRule>();
            this.PropertyMapRules.Add(new DefaultPropertyMappingRule());
            this.DictionaryMappingRules = new List<DictionaryMappingRule>();
            this.DictionaryMappingRules.Add(new DictionaryMappingRule(DictionaryMappingDirection.DictionaryToObject, typeof(Object), TypeFilterCondition.Inherit));
            this.DictionaryMappingRules.Add(new DictionaryMappingRule(DictionaryMappingDirection.ObjectToDictionary, typeof(Object), TypeFilterCondition.Inherit));

            this.MaxCallStack = 100;
            this.DictionaryKeyStringComparer = StringComparer.OrdinalIgnoreCase;
            this.NullPropertyMapMode = NullPropertyMapMode.NewObject;
            this.CollectionElementMapMode = CollectionElementMapMode.NewObject;
        }
        private static MethodInfo GetMethodInfo(String name)
        {
            return typeof(ObjectMapConfig).GetMethods().Where(el => el.GetCustomAttributes().Any(attr => attr is ObjectMapConfigMethodAttribute && ((ObjectMapConfigMethodAttribute)attr).Name == name)).FirstOrDefault();
        }
        private MappingContext CreateMappingContext()
        {
            return new MappingContext(this.DictionaryKeyStringComparer, this.NullPropertyMapMode, this.CollectionElementMapMode);
        }

        public TTarget Map<TSource, TTarget>(TSource source, TTarget target)
        {
            return this.Map(source, target, this.CreateMappingContext());
        }
        public TTarget MapOrNull<TSource, TTarget>(TSource source, Func<TTarget> targetConstructor)
            where TTarget : class
        {
            if (source == null) return null;
            return this.Map(source, targetConstructor(), this.CreateMappingContext());
        }
        public TTarget MapOrNull<TSource, TTarget>(TSource source, TTarget target)
            where TTarget : class
        {
            if (source == null) return null;
            return this.Map(source, target, this.CreateMappingContext());
        }
        public TTarget Map<TSource, TTarget>(TSource source, TTarget target, MappingContext context)
        {
            if (source == null || target == null) { return this.CallPostAction(source, target); }
            if (source is IDataReader)
            {
                return this.MapFromDataReader(source as IDataReader, target, context);
            }
            var md = this.GetMethod<TSource, TTarget>();
            TTarget result = target;
            if (md != null)
            {
                Exception exception = null;
                try
                {
                    result = md.Map(this, source, result, context);
                }
                catch (VerificationException ex)
                {
                    exception = ex;
                }
                catch (TargetInvocationException ex)
                {
                    exception = ex.InnerException;
                }
                if (exception != null)
                {
                    throw new ObjectMapFailureException("Generated map method was failed.Maybe HigLabo.Mapper bug."
                        + "Please notify SouceObject,TargetObject class of this ObjectMapFailureException object to auther."
                        + "We will fix it." + Environment.NewLine
                        + String.Format("SourceType={0}, TargetType={1}", source.GetType().Name, target.GetType().Name)
                        , source, target, exception);
                }
                if (context.NullPropertyMapMode != NullPropertyMapMode.None ||
                    context.CollectionElementMapMode != CollectionElementMapMode.None)
                {
                    md.MapChildObject?.Invoke(this, source, target, context);
                }
            }
            return this.CallPostAction(source, result);
        }
        [ObjectMapConfigMethod(Name = "Map")]
        public TTarget MapInternal<TSource, TTarget>(TSource source, TTarget target, MappingContext context)
        {
            context.CallStackCount++;
            TTarget result = target;
            if (source != null && target != null)
            {
                var md = this.GetMethod<TSource, TTarget>();
                if (md.Map != null)
                {
                    result = md.Map(this, source, result, context);
                }
                if (md.MapChildObject != null)
                {
                    md.MapChildObject(this, source, target, context);
                }
            }
            context.CallStackCount--;
            return this.CallPostAction(source, result);
        }
        private TTarget MapFromDataReader<TTarget>(IDataReader source, TTarget target, MappingContext context)
        {
            Dictionary<String, Object> d = new Dictionary<String, Object>(context.DictionaryKeyStringComparer);
            d.SetValues((IDataReader)source);
            return this.Map(d, target, context);
        }
        [ObjectMapConfigMethod(Name = "MapTo")]
        public ICollection<TTarget> MapTo<TSource, TTarget>(IEnumerable<TSource> source, ICollection<TTarget> target)
            where TTarget : new()
        {
            return this.MapTo(source, target, () => new TTarget());
        }
        public ICollection<TTarget> MapTo<TSource, TTarget>(IEnumerable<TSource> source, ICollection<TTarget> target
            , Func<TTarget> elementConstructor)
        {
            if (source != null && target != null)
            {
                foreach (var item in source)
                {
                    var o = this.Map(item, elementConstructor());
                    target.Add(o);
                }
            }
            return target;
        }
        [ObjectMapConfigMethod(Name = "MapReference")]
        public ICollection<TTarget> MapReference<TSource, TTarget>(IEnumerable<TSource> source, ICollection<TTarget> target)
           where TSource : TTarget
        {
            if (source != null && target != null)
            {
                foreach (var item in source)
                {
                    target.Add(item);
                }
            }
            return target;
        }

        public void RemovePropertyMap<TSource, TTarget>()
        {
            this.RemovePropertyMap<TSource, TTarget>(propertyMap => true, null);
        }
        public void RemovePropertyMap<TSource, TTarget>(IEnumerable<String> propertyNames)
        {
            this.RemovePropertyMap<TSource, TTarget>(propertyMap => propertyNames.Contains(propertyMap.Target.Name), null);
        }
        public void RemovePropertyMap<TSource, TTarget>(params String[] propertyNames)
        {
            this.RemovePropertyMap<TSource, TTarget>(propertyMap => propertyNames.Contains(propertyMap.Target.Name), null);
        }
        public void RemovePropertyMap<TSource, TTarget>(IEnumerable<String> propertyNames, Action<TSource, TTarget> action)
        {
            this.RemovePropertyMap<TSource, TTarget>(propertyMap => propertyNames.Contains(propertyMap.Target.Name), action);
        }
        public void RemovePropertyMap<TSource, TTarget>(Func<PropertyMap, Boolean> selector)
        {
            this.RemovePropertyMap<TSource, TTarget>(selector, null);
        }
        public void RemovePropertyMap<TSource, TTarget>(Func<PropertyMap, Boolean> selector, Action<TSource, TTarget> action)
        {
            this.ReplacePropertyMap(selector, action);
        }

        public void ReplacePropertyMap<TSource, TTarget>(Action<TSource, TTarget> action)
        {
            this.ReplacePropertyMap<TSource, TTarget>(propertyMap => true, action);
        }
        private void ReplacePropertyMap<TSource, TTarget>(Func<PropertyMap, Boolean> selector, Action<TSource, TTarget> action)
        {
            var key = new ObjectMapTypeInfo(typeof(TSource), typeof(TTarget));
            var mappings = this.CreatePropertyMaps(key.Source, key.Target);
            var startIndex = mappings.Count - 1;
            for (int i = startIndex; i > -1; i--)
            {
                if (selector(mappings[i]) == true)
                {
                    mappings.RemoveAt(i);
                }
            }
            if (mappings.Count == 0)
            {
                _Methods[key] = null;
            }
            else
            {
                var md = this.CreateMapMethodInfo<TSource, TTarget>(key, mappings);
                _Methods[key] = md;
            }
            this.AddPostAction(action);
        }

        public void AddPostAction<T>(Action<T, T> action)
        {
            this.AddPostAction<T, T>(action);
        }
        public void AddPostAction<T>(Func<T, T, T> action)
        {
            this.AddPostAction<T, T>(action);
        }
        public void AddPostAction<TSource, TTarget>(Action<TSource, TTarget> action)
        {
            if (action == null) { return; }
            Func<TSource, TTarget, TTarget> f = (source, target) =>
            {
                action(source, target);
                return target;
            };
            this.AddPostAction(TypeFilterCondition.Inherit, TypeFilterCondition.Inherit, f);
        }
        public void AddPostAction<TSource, TTarget>(Func<TSource, TTarget, TTarget> action)
        {
            this.AddPostAction(TypeFilterCondition.Inherit, TypeFilterCondition.Inherit, action);
        }
        public void AddPostAction<TSource, TTarget>(TypeFilterCondition sourceCondition, TypeFilterCondition targetCondition, Func<TSource, TTarget, TTarget> action)
        {
            if (action == null) { return; }

            var condition = new MappingCondition();
            condition.Source.Type = typeof(TSource);
            condition.Source.TypeFilterCondition = sourceCondition;
            condition.Target.Type = typeof(TTarget);
            condition.Target.TypeFilterCondition = targetCondition;
            _PostActions.Add(new MapPostAction(condition, (Delegate)action));
        }
        private TTarget CallPostAction<TSource, TTarget>(TSource source, TTarget target)
        {
            if (_PostActions.Count == 0) { return target; }

            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            for (int i = 0; i < _PostActions.Count; i++)
            {
                if (_PostActions[i].Condition.Match(sourceType, targetType) == false) { continue; }
                var f = (Func<TSource, TTarget, TTarget>)_PostActions[i].Action;
                return f(source, target);
            }
            return target;
        }

        public void CreateMap<TSource, TTarget>()
        {
            var md = this.GetMethod<TSource, TTarget>();
        }
        private MapMethodInfo<TSource, TTarget> GetMethod<TSource, TTarget>()
        {
            Object md = null;
            var key = new ObjectMapTypeInfo(typeof(TSource), typeof(TTarget));
            if (_Methods.TryGetValue(key, out md) == false)
            {
                var l = this.CreatePropertyMaps(key.Source, key.Target);
                md = this.CreateMapMethodInfo<TSource, TTarget>(key, l);
                _Methods[key] = md;
                //Int32 loopCount = 0;
                //while (true)
                //{
                //    if (_Methods.TryAdd(key, md)) { break; }
                //    loopCount++;
                //    if (loopCount > 3) { throw new InvalidOperationException("CreateMethod failed due to race condition.Please try later."); }
                //}
            }
            return (MapMethodInfo<TSource, TTarget>)md;
        }

        private List<PropertyMap> CreatePropertyMaps(Type sourceType, Type targetType)
        {
            List<PropertyMap> l = new List<PropertyMap>();
            var sourceTypes = new List<Type>();
            sourceTypes.Add(sourceType);
            sourceTypes.AddRange(sourceType.GetBaseClasses());
            sourceTypes.AddRange(sourceType.GetInterfaces());
            var targetTypes = new List<Type>();
            targetTypes.Add(targetType);
            targetTypes.AddRange(targetType.GetBaseClasses());
            targetTypes.AddRange(targetType.GetInterfaces());
            List<PropertyInfo> sourceProperties = new List<PropertyInfo>();
            List<PropertyInfo> targetProperties = new List<PropertyInfo>();
            foreach (var item in sourceTypes)
            {
                if (item == typeof(Object)) { continue; }

                foreach (var p in item.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(el => el.GetIndexParameters().Length == 0))
                {
                    sourceProperties.Add(p);
                }
            }
            foreach (var item in targetTypes)
            {
                if (item == typeof(Object)) { continue; }

                foreach (var p in item.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(el => el.GetIndexParameters().Length == 0))
                {
                    if (p.SetMethod == null) { continue; }
                    targetProperties.Add(p);
                }
            }
            //Object --> Object 
            foreach (PropertyMappingRule rule in this.PropertyMapRules)
            {
                //Find target property by rules.Validate rule is match to sourceType,targetType.
                if (rule.Condition.Match(sourceType, targetType) == false) { continue; }

                List<PropertyInfo> addedTargetPropertyMapInfo = new List<PropertyInfo>();
                foreach (var piTarget in targetProperties)
                {
                    //Search property that meet condition
                    foreach (var piSource in sourceProperties)
                    {
                        if (rule.Match(piSource, piTarget) == false) { continue; }

                        l.Add(new PropertyMap(piSource, piTarget));
                        addedTargetPropertyMapInfo.Add(piTarget);
                        break;
                    }
                }
                //Remove added target property
                for (int i = 0; i < addedTargetPropertyMapInfo.Count; i++)
                {
                    targetProperties.Remove(addedTargetPropertyMapInfo[i]);
                }
            }
            //Dictionary<String, T> --> Object. 
            var piSourceItem = sourceType.GetProperty("Item", new Type[] { typeof(String) });
            if (piSourceItem != null)
            {
                foreach (var rule in this.DictionaryMappingRules
                    .Where(el => el.Direction == DictionaryMappingDirection.DictionaryToObject))
                {
                    if (rule.Condition.Match(targetType) == false) { continue; }

                    foreach (var piTarget in targetProperties)
                    {
                        foreach (var key in rule.GetIndexedKeys(piTarget.Name))
                        {
                            l.Add(new PropertyMap(piSourceItem, key, piTarget));
                        }
                    }
                }
            }
            //Object --> Dictionary<String, T>. 
            var piTargetItem = targetType.GetProperty("Item", new Type[] { typeof(String) });
            if (piTargetItem != null)
            {
                foreach (var rule in this.DictionaryMappingRules
                    .Where(el => el.Direction == DictionaryMappingDirection.ObjectToDictionary))
                {
                    if (rule.Condition.Match(sourceType) == false) { continue; }

                    foreach (var piSource in sourceProperties)
                    {
                        foreach (var key in rule.GetIndexedKeys(piSource.Name))
                        {
                            l.Add(new PropertyMap(piSource, piTargetItem, key));
                        }
                    }
                }
            }
            return l;
        }
        private MapMethodInfo<TSource, TTarget> CreateMapMethodInfo<TSource, TTarget>(ObjectMapTypeInfo key, IEnumerable<PropertyMap> propertyMapInfo)
        {
            var mdInfo = new MapMethodInfo<TSource, TTarget>();
            mdInfo.Map = (Func<ObjectMapConfig, TSource, TTarget, MappingContext, TTarget>)this.CreateMapPropertyMethod(key.Source, key.Target, propertyMapInfo);

            var l = new List<PropertyMap>();
            foreach (var item in propertyMapInfo)
            {
                var toXXXMethod = GetTypeConverterMethodInfo(item.Target.ActualType);
                if (item.Source.ActualType == item.Target.ActualType &&
                    IsDirectSetValue(item.Source.ActualType))
                {
                    continue;
                }
                if (toXXXMethod != null) { continue; }
                if (item.Target.IsIndexedProperty) { continue; }

                l.Add(item);
            }            
            if (l.Count > 0)
            {
                mdInfo.MapChildObject = (Func<ObjectMapConfig, TSource, TTarget, MappingContext, TTarget>)this.CreateMapChildObjectMethod(key.Source, key.Target, l);
            }
            return mdInfo;
        }
        /// <summary>
        /// ***********************************************************************
        /// source.P1 --> target.P1;
        /// source.P1 --> target["P1"];
        /// source["P1"] --> target.P1;
        /// source["P1"] --> target["P1"];
        /// context --> MappingContext.
        /// if (typeof(source) == typeof(target))
        /// {
        ///     target.P1 = source.P1;
        /// }
        /// else if (Use TypeConverter for primitive types)
        /// {
        ///     var converted = converter.ToXXX(source.P1);
        ///     if (converted != null)
        ///     {
        ///         target.P1 = converted;
        ///         return;
        ///     }
        /// }
        /// else
        /// {
        ///     target.P1 = source["P1"];
        ///     return;
        /// }
        /// //Null property handling...
        /// if (target property is Class)
        /// {
        ///     switch (context.NullPropertyMapMode)
        ///     {
        ///         case NullPropertyMapMode.NewObject: target.P1 = new XXX(); break;
        ///         case NullPropertyMapMode.CopyReference: 
        ///         {
        ///             if (typeof(source) inherit from typeof(parent))
        ///             {
        ///                 target.P1 = source.P1; 
        ///             }
        ///             break;
        ///         }
        ///     }
        ///     if (source type is IEnumerable and target type is ICollection)
        ///     {
        ///         switch (context.CollectionElementMapmode)
        ///         {
        ///             case CollectionElementMapmode.NewObject: this.MapTo(source, target); break;
        ///             case CollectionElementMapmode.CopyReference: this.MapReference(source, target); break;
        ///         }
        ///     }
        /// }
        /// target.P1 = source.P1.Map(target.P1);
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="propertyMapInfo"></param>
        /// <returns></returns>
        private Delegate CreateMapPropertyMethod(Type sourceType, Type targetType, IEnumerable<PropertyMap> propertyMapInfo)
        {
            DynamicMethod dm = new DynamicMethod("MapProperty", targetType, new[] { typeof(ObjectMapConfig), sourceType, targetType, typeof(MappingContext) });
            ILGenerator il = dm.GetILGenerator();

            var typeConverterVal = il.DeclareLocal(typeof(TypeConverter));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, _ObjectMapConfig_TypeConverterProperty_GetMethod);
            il.SetLocal(typeConverterVal);//ObjectMapConfig.TypeConverter

            Label methodEnd = il.DefineLabel();
            foreach (var item in propertyMapInfo)
            {
                Label setValueStartLabel = il.DefineLabel();
                Label setNullToTargetLabel = il.DefineLabel();
                Label endOfCode = il.DefineLabel();
                var getMethod = item.Source.PropertyInfo.GetGetMethod();
                var setMethod = item.Target.PropertyInfo.GetSetMethod();
                var sourceVal = il.DeclareLocal(item.Source.ActualType);
                var targetVal = il.DeclareLocal(item.Target.ActualType);
      
                #region val sourceVal = source.P1; //GetValue from SourceObject. 

                //Get value from source property.
                if (item.Source.IsIndexedProperty == true)
                {
                    // var sourceVal = source["P1"];
                    #region Dictionary<String, String> or Dictionary<String, Object>
                    //Call TryGetValue method to avoid KeyNotFoundException
                    if (sourceType.IsInheritanceFrom(typeof(Dictionary<String, String>)) == true ||
                        sourceType.IsInheritanceFrom(typeof(Dictionary<String, Object>)) == true)
                    {
                        //Call ContainsKey method.If key does not exist, exit method.
                        var containsKey = sourceType.GetMethod("ContainsKey");
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldstr, item.Source.IndexedPropertyKey);
                        il.Emit(OpCodes.Callvirt, containsKey);
                        il.Emit(OpCodes.Brfalse, endOfCode); //ContainsKey=false --> Exit method without do anything.
                    }
                    //source[string key]
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldstr, item.Source.IndexedPropertyKey);
                    il.Emit(OpCodes.Callvirt, getMethod);
                    #endregion
                }
                else
                {
                    // var sourceVal = source.P1;
                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Ldarga_S, 1);
                        il.Emit(OpCodes.Call, getMethod);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Callvirt, getMethod);
                    }
                }
                //Check source.P1 is null. If null, goto target.P1 = null.
                if (item.Source.CanBeNull == true)
                {
                    var sourceValN = il.DeclareLocal(item.Source.PropertyType);
                    il.SetLocal(sourceValN);

                    if (item.Source.IsNullableT == true)
                    {
                        #region if (source.P1.HasValue == false)
                        il.LoadLocala(sourceValN);
                        il.Emit(OpCodes.Call, item.Source.PropertyType.GetProperty("HasValue").GetGetMethod());
                        il.Emit(OpCodes.Brfalse, setNullToTargetLabel); //null --> set target null

                        //sourceVal.Value
                        il.LoadLocala(sourceValN);
                        il.Emit(OpCodes.Call, item.Source.PropertyType.GetMethod("GetValueOrDefault", Type.EmptyTypes));
                        #endregion
                    }
                    else if (item.Source.CanBeNull == true)
                    {
                        #region if (source.P1 == null)
                        il.LoadLocal(sourceValN);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Brtrue, setNullToTargetLabel); //null --> set target null

                        il.LoadLocal(sourceValN);
                        #endregion
                    }
                }
                //store sourceVal (never be null)
                il.SetLocal(sourceVal);
                #endregion

                #region var convertedVal = TypeConverter.ToXXX(sourceVal); //Convert value to target type.
                LocalBuilder convertedVal = null;
                var toXXXMethod = GetTypeConverterMethodInfo(item.Target.ActualType);
                if (item.Source.ActualType == item.Target.ActualType &&
                    IsDirectSetValue(item.Source.ActualType))
                {
                    #region target.P1 = source.P1;
                    il.LoadLocal(sourceVal);
                    il.SetLocal(targetVal);
                    #endregion
                }
                else if (toXXXMethod != null)
                {
                    #region target.P1 = TypeConverter.ToXXX(sourceVal);
                    //Call TypeConverter.ToXXX(sourceVal);
                    il.LoadLocal(typeConverterVal);//MapConfig.TypeConverter
                    il.LoadLocal(sourceVal);
                    if (item.Source.ActualType.IsValueType == true)
                    {
                        il.Emit(OpCodes.Box, item.Source.ActualType);
                    }
                    il.Emit(OpCodes.Callvirt, toXXXMethod);
                    #endregion

                    Label ifConvertedValueNotNullBlock = il.DefineLabel();
                    if (item.Target.PropertyType.IsClass)
                    {
                        //ToString, ToEncoding
                        il.SetLocal(targetVal);

                        il.LoadLocal(targetVal);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Brtrue_S, setNullToTargetLabel);
                    }
                    else
                    {
                        #region if (convertedVal.HasValue) {...}
                        Type targetTypeN = typeof(Nullable<>).MakeGenericType(item.Target.ActualType);
                        convertedVal = il.DeclareLocal(targetTypeN);
                        il.SetLocal(convertedVal);
                        il.LoadLocala(convertedVal);
                        il.Emit(OpCodes.Call, targetTypeN.GetProperty("HasValue").GetGetMethod());
                        il.Emit(OpCodes.Brfalse_S, setNullToTargetLabel);
                        {
                            //GetValue
                            il.LoadLocala(convertedVal);
                            il.Emit(OpCodes.Call, targetTypeN.GetMethod("GetValueOrDefault", Type.EmptyTypes));
                            il.SetLocal(targetVal);
                        }
                        #endregion
                    }
                }
                else
                {
                    #region target["P1"] = source.P1; 
                    if (item.Target.IsIndexedProperty)
                    {
                        il.LoadLocal(sourceVal);
                        if (item.Source.ActualType.IsValueType == true)
                        {
                            //Boxing to Object
                            il.Emit(OpCodes.Box, item.Source.ActualType);
                        }
                        il.SetLocal(targetVal);
                        il.Emit(OpCodes.Br, setValueStartLabel);
                    }
                    #endregion
                }
                #endregion

                il.MarkLabel(setValueStartLabel);
                #region target.P1 = source.P1; //Set value to TargetProperty
                if (targetType.IsClass) { il.Emit(OpCodes.Ldarg_2); }
                else if (targetType.IsValueType) { il.Emit(OpCodes.Ldarga_S, 2); }
                if (item.Target.IsIndexedProperty == true)
                {
                    //target["P1"] = source.P1;
                    il.Emit(OpCodes.Ldstr, item.Target.IndexedPropertyKey);
                }
                il.LoadLocal(targetVal);
                if (item.Target.IsNullableT == true)
                {
                    //new Nullable<T>(new T());
                    il.Emit(OpCodes.Newobj, item.Target.PropertyType.GetConstructor(new Type[] { item.Target.ActualType }));
                }
                if (targetType.IsClass) { il.Emit(OpCodes.Callvirt, setMethod); }
                else if (targetType.IsValueType) { il.Emit(OpCodes.Call, setMethod); }
                il.Emit(OpCodes.Br_S, endOfCode);
                #endregion

                il.MarkLabel(setNullToTargetLabel);
                #region target.P1 = null. //Set Null to Target property.
                if (item.Target.CanBeNull == true)
                {
                    il.Emit(OpCodes.Ldarg_2);
                    if (item.Target.IsIndexedProperty == true)
                    {
                        //target["P1"] = source.P1;
                        il.Emit(OpCodes.Ldstr, item.Target.IndexedPropertyKey);
                    }
                    if (item.Target.IsNullableT == true)
                    {
                        var targetValN = il.DeclareLocal(item.Target.PropertyType);
                        il.LoadLocala(targetValN);
                        il.Emit(OpCodes.Initobj, item.Target.PropertyType);
                        il.LoadLocal(targetValN);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldnull);
                    }
                    il.Emit(OpCodes.Callvirt, setMethod);
                }
                #endregion

                il.MarkLabel(endOfCode);
            }
            il.MarkLabel(methodEnd);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ret);

            var f = typeof(Func<,,,,>);
            var gf = f.MakeGenericType(typeof(ObjectMapConfig), sourceType, targetType, typeof(MappingContext), targetType);
            return dm.CreateDelegate(gf);
        }
        private Delegate CreateMapChildObjectMethod(Type sourceType, Type targetType, IEnumerable<PropertyMap> propertyMapInfo)
        {
            DynamicMethod dm = new DynamicMethod("MapChildObjectMethod", targetType, new[] { typeof(ObjectMapConfig), sourceType, targetType, typeof(MappingContext) });
            ILGenerator il = dm.GetILGenerator();

            var collectionMapMode = il.DeclareLocal(typeof(CollectionElementMapMode));
            il.Emit(OpCodes.Ldarg_3);//MappingContext
            il.Emit(OpCodes.Callvirt, _MappingContext_CollectionElementMapMode_GetMethod);
            il.SetLocal(collectionMapMode);

            var nullPropertyMapMode = il.DeclareLocal(typeof(NullPropertyMapMode));
            il.Emit(OpCodes.Ldarg_3);//MappingContext
            il.Emit(OpCodes.Callvirt, _MappingContext_NullPropertyMapMode_GetMethod);
            il.SetLocal(nullPropertyMapMode);

            Label methodEnd = il.DefineLabel();
            foreach (var item in propertyMapInfo)
            {
                Label endOfCode = il.DefineLabel();
                var getMethod = item.Source.PropertyInfo.GetGetMethod();
                var setMethod = item.Target.PropertyInfo.GetSetMethod();
                var sourceVal = il.DeclareLocal(item.Source.ActualType);
                var targetVal = il.DeclareLocal(item.Target.ActualType);

                #region val sourceVal = source.P1; //GetValue from SourceObject. 

                //Get value from source property.
                if (item.Source.IsIndexedProperty == true)
                {
                    // var sourceVal = source["P1"];
                    #region Dictionary<String, String> or Dictionary<String, Object>
                    //Call TryGetValue method to avoid KeyNotFoundException
                    if (sourceType.IsInheritanceFrom(typeof(Dictionary<String, String>)) == true ||
                        sourceType.IsInheritanceFrom(typeof(Dictionary<String, Object>)) == true)
                    {
                        //Call ContainsKey method.If key does not exist, exit method.
                        var containsKey = sourceType.GetMethod("ContainsKey");
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldstr, item.Source.IndexedPropertyKey);
                        il.Emit(OpCodes.Callvirt, containsKey);
                        il.Emit(OpCodes.Brfalse, endOfCode); //ContainsKey=false --> Exit method without do anything.
                    }
                    //source[string key]
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldstr, item.Source.IndexedPropertyKey);
                    il.Emit(OpCodes.Callvirt, getMethod);
                    #endregion
                }
                else
                {
                    // var sourceVal = source.P1;
                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Ldarga_S, 1);
                        il.Emit(OpCodes.Call, getMethod);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Callvirt, getMethod);
                    }
                }
                //Check source.P1 is null. If null, goto target.P1 = null.
                if (item.Source.CanBeNull == true)
                {
                    if (item.Source.IsNullableT == true)
                    {
                        #region if (source.P1.HasValue == false)
                        var sourceValN = il.DeclareLocal(item.Source.PropertyType);

                        il.SetLocal(sourceValN);
                        //if (sourceValue.HasValue == true)
                        il.LoadLocala(sourceValN);
                        il.Emit(OpCodes.Call, item.Source.PropertyType.GetProperty("HasValue").GetGetMethod());
                        il.Emit(OpCodes.Brfalse, endOfCode); //null --> set target null
                        il.LoadLocala(sourceValN);
                        //sourceVal.Value
                        il.Emit(OpCodes.Call, item.Source.PropertyType.GetMethod("GetValueOrDefault", Type.EmptyTypes));
                        #endregion
                    }
                    else if (item.Source.CanBeNull == true)
                    {
                        #region if (source.P1 == null)
                        Type sourceTypeN = item.Source.ActualType;
                        var sourceValN = il.DeclareLocal(sourceTypeN);
                        il.SetLocal(sourceValN);
                        //if (sourceValue == null)
                        il.LoadLocal(sourceValN);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Brtrue, endOfCode); //null --> set target null
                        il.LoadLocal(sourceValN);
                        #endregion
                    }
                }
                //store sourceVal (never be null)
                il.SetLocal(sourceVal);
                #endregion

                #region if (target.P1 == null) { //Create Object }
                if (item.Source.PropertyType.IsClass && item.Target.PropertyType.IsClass)
                {
                    var currentTargetVal = il.DeclareLocal(item.Target.PropertyType);
                    #region if (target.P1 == null) { //Create Object }
                    var mapToLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Callvirt, item.Target.PropertyInfo.GetGetMethod());
                    il.SetLocal(currentTargetVal);

                    il.LoadLocal(currentTargetVal);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ceq);
                    Label ifTargetIsNull = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue, ifTargetIsNull);
                    {
                        il.Emit(OpCodes.Ldarg_0);//ObjectMapConfig instance
                        il.LoadLocal(sourceVal);
                        il.LoadLocal(currentTargetVal);
                        il.Emit(OpCodes.Ldarg_3);//MappingContext
                        il.Emit(OpCodes.Callvirt, _MapMethod.MakeGenericMethod(item.Source.ActualType, item.Target.ActualType));
                        il.SetLocal(targetVal);

                        il.Emit(OpCodes.Br, mapToLabel);
                    }
                    il.MarkLabel(ifTargetIsNull);
                    {
                        #region if (context.NullPropertyMapMode == NullPropertyMapMode.NewObject) { target = new TTarget(); }
                        il.LoadLocal(nullPropertyMapMode);
                        il.Emit(OpCodes.Ldc_I4, (Int32)NullPropertyMapMode.NewObject);
                        il.Emit(OpCodes.Ceq);
                        Label ifNullMapModeIsNotNewObject = il.DefineLabel();
                        il.Emit(OpCodes.Brfalse_S, ifNullMapModeIsNotNewObject);
                        {
                            var constructor = item.Target.ActualType.GetConstructor(Type.EmptyTypes);
                            if (constructor != null && constructor.IsPublic)
                            {
                                il.Emit(OpCodes.Ldarg_0);//ObjectMapConfig instance
                                il.LoadLocal(sourceVal);
                                il.Emit(OpCodes.Newobj, constructor);
                                il.Emit(OpCodes.Ldarg_3);//MappingContext
                                il.Emit(OpCodes.Callvirt, _MapMethod.MakeGenericMethod(item.Source.ActualType, item.Target.ActualType));
                                il.SetLocal(targetVal);
                            }
                        }
                        il.MarkLabel(ifNullMapModeIsNotNewObject);
                        #endregion

                        #region if (context.NullPropertyMapMode == NullPropertyMapMode.CopyReference) { target = source; }
                        if (item.Source.ActualType.IsInheritanceFrom(item.Target.ActualType))
                        {
                            il.LoadLocal(nullPropertyMapMode);
                            il.Emit(OpCodes.Ldc_I4, (Int32)NullPropertyMapMode.CopyReference);
                            il.Emit(OpCodes.Ceq);
                            Label ifNullMapModeIsNotCopyReference = il.DefineLabel();
                            il.Emit(OpCodes.Brfalse_S, ifNullMapModeIsNotCopyReference);
                            {
                                il.LoadLocal(sourceVal);
                                il.SetLocal(targetVal);
                            }
                            il.MarkLabel(ifNullMapModeIsNotCopyReference);
                        }
                        #endregion
                    }
                    #endregion

                    il.MarkLabel(mapToLabel);
                    #region Call MapMapTo,MapReference method to collection.
                    if (IsEnumerableToCollection(item))
                    {
                        var sInterface = item.Source.ActualType.GetInterfaces().FirstOrDefault(el => el.FullName.StartsWith(System_Collections_Generic_IEnumerable_1));
                        var tInterface = item.Target.ActualType.GetInterfaces().FirstOrDefault(el => el.FullName.StartsWith(System_Collections_Generic_IEnumerable_1));
                        if (sInterface != null && tInterface != null)
                        {
                            var sourceElementType = sInterface.GenericTypeArguments[0];
                            var targetElementType = tInterface.GenericTypeArguments[0];
                            if (targetElementType.GetConstructor(Type.EmptyTypes) != null)
                            {
                                #region if (mode == CollectionElementMapMode.NewObject) { source.P1.MapTo(target); }
                                il.LoadLocal(collectionMapMode);
                                il.Emit(OpCodes.Ldc_I4, (Int32)CollectionElementMapMode.NewObject);
                                il.Emit(OpCodes.Ceq);
                                Label ifMapModeIsNotNewObject = il.DefineLabel();
                                il.Emit(OpCodes.Brfalse_S, ifMapModeIsNotNewObject); //_MapToMethod
                                {
                                    il.Emit(OpCodes.Ldarg_0);//ObjectMapConfig instance
                                    il.LoadLocal(sourceVal);
                                    il.LoadLocal(targetVal);
                                    il.Emit(OpCodes.Call, _MapToMethod.MakeGenericMethod(sourceElementType, targetElementType));
                                    il.Emit(OpCodes.Pop);
                                }
                                il.MarkLabel(ifMapModeIsNotNewObject);
                                #endregion
                            }

                            if (sourceElementType.IsInheritanceFrom(targetElementType))
                            {
                                #region if (mode == CollectionElementMapMode.CopyReference) { source.P1.MapReference(target); }
                                il.LoadLocal(collectionMapMode);
                                il.Emit(OpCodes.Ldc_I4, (Int32)CollectionElementMapMode.CopyReference);
                                il.Emit(OpCodes.Ceq);
                                Label ifMapModeIsNotCopyReference = il.DefineLabel();
                                il.Emit(OpCodes.Brfalse_S, ifMapModeIsNotCopyReference); //_MapReferenceMethod
                                {
                                    il.Emit(OpCodes.Ldarg_0);//ObjectMapConfig instance
                                    il.LoadLocal(sourceVal);
                                    il.LoadLocal(targetVal);
                                    il.Emit(OpCodes.Call, _MapReferenceMethod.MakeGenericMethod(sourceElementType, targetElementType));
                                    il.Emit(OpCodes.Pop);
                                }
                                il.MarkLabel(ifMapModeIsNotCopyReference);
                                #endregion
                            }
                        }
                    }
                    #endregion

                    #region this.Map(source.P1, target.Map)
                    //il.Emit(OpCodes.Ldarg_0);//ObjectMapConfig instance
                    //il.LoadLocal(sourceVal);
                    //il.LoadLocal(targetVal);
                    //il.Emit(OpCodes.Ldarg_3);//MappingContext
                    //il.Emit(OpCodes.Call, _MapMethod.MakeGenericMethod(item.Source.ActualType, item.Target.ActualType));
                    //il.SetLocal(targetVal);
                    #endregion
                }
                #endregion

                #region target.P1 = source.P1; //Set value to TargetProperty
                if (targetType.IsClass) { il.Emit(OpCodes.Ldarg_2); }
                else if (targetType.IsValueType) { il.Emit(OpCodes.Ldarga_S, 2); }
                if (item.Target.IsIndexedProperty == true)
                {
                    //target["P1"] = source.P1;
                    il.Emit(OpCodes.Ldstr, item.Target.IndexedPropertyKey);
                }
                il.LoadLocal(targetVal);
                if (item.Target.IsNullableT == true)
                {
                    //new Nullable<T>(new T());
                    il.Emit(OpCodes.Newobj, item.Target.PropertyType.GetConstructor(new Type[] { item.Target.ActualType }));
                }
                if (targetType.IsClass) { il.Emit(OpCodes.Callvirt, setMethod); }
                else if (targetType.IsValueType) { il.Emit(OpCodes.Call, setMethod); }
                il.Emit(OpCodes.Br_S, endOfCode);
                #endregion

                il.MarkLabel(endOfCode);
                il.Emit(OpCodes.Nop);
            }
            il.MarkLabel(methodEnd);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ret);

            var f = typeof(Func<,,,,>);
            var gf = f.MakeGenericType(typeof(ObjectMapConfig), sourceType, targetType, typeof(MappingContext), targetType);
            return dm.CreateDelegate(gf);
        }

        private static Boolean IsEnumerableToCollection(PropertyMap propertyMap)
        {
            var sType = propertyMap.Source.ActualType;
            var tType = propertyMap.Target.ActualType;

            if (propertyMap.Source.IsIndexedProperty == true || propertyMap.Target.IsIndexedProperty == true) { return false; }
            if (sType.GenericTypeArguments.Length == 0 || tType.GenericTypeArguments.Length == 0) { return false; }
            //Never use lambda expresion to improve performance.To avoid object creation.
            foreach (var sInterface in sType.GetInterfaces())
            {
                if (sInterface.FullName.StartsWith(System_Collections_Generic_IEnumerable_1))
                {
                    foreach (var tInterface in tType.GetInterfaces())
                    {
                        if (tInterface.FullName.StartsWith(System_Collections_Generic_ICollection_1)) { return true; }
                    }
                }
            }
            return false;
        }
        private static Boolean IsDirectSetValue(Type type)
        {
            if (type == typeof(String)) return true;
            if (type.IsValueType) return true;
            return false;
        }
        private static MethodInfo GetTypeConverterMethodInfo(Type type)
        {
            MethodInfo md = null;
            if (type.IsEnum)
            {
                if (_TypeConverter_ToEnumMethods.TryGetValue(type, out md) == true) { return md; }
                md = typeof(TypeConverter).GetMethod("ToEnum", new Type[] { typeof(Object) }).MakeGenericMethod(type);
                _TypeConverter_ToEnumMethods.TryAdd(type, md);
                return md;
            }
            else
            {
                if (_TypeConverter_ToTypeMethods.TryGetValue(type, out md) == true) { return md; }
            }
            return null;
        }
    }
}

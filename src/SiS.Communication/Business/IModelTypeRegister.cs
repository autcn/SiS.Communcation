using System;
using System.Reflection;
using System.Collections.Generic;

namespace SiS.Communication.Business
{
    /// <summary>
    /// Provide methods to register types which will be used in tcp model transmission.
    /// </summary>
    public interface IModelTypeRegister
    {
        /// <summary>
        /// Register a model type with name and Type.
        /// </summary>
        /// <param name="typeName">The type name to register</param>
        /// <param name="type">The model type to register</param>
        void Register(string typeName, Type type);

        /// <summary>
        /// Register a model type with the Type's default name.
        /// </summary>
        /// <param name="type">The model type to register</param>
        void Register(Type type);

        /// <summary>
        /// Register model types with the Type's default name.
        /// </summary>
        /// <param name="types">The model types to register</param>
        void Register(IEnumerable<Type> types);

        /// <summary>
        /// Register a model type with specific name.
        /// </summary>
        /// <typeparam name="T">The type based on the type of ModelMessageBase</typeparam>
        /// <param name="typeName">The type name to register</param>
        void Register<T>(string typeName) where T : ModelMessageBase;

        /// <summary>
        /// Register a model type with the Type's default name.
        /// </summary>
        /// <typeparam name="T">The type based on the type of ModelMessageBase</typeparam>
        void Register<T>() where T : ModelMessageBase;

        /// <summary>
        /// Register types in specific assembly which based on the type of ModelMessageBase.
        /// </summary>
        /// <param name="assembly">The assembly that contains types derived from ModelMessageBase.</param>
        void Register(Assembly assembly);


        /// <summary>
        /// Register generic types.
        /// </summary>
        /// <param name="genericType">The generic type to register</param>
        /// <param name="dataTypes">The data types to be combined with generic type.</param>
        void RegisterGeneric(Type genericType, params Type[] dataTypes);

        /// <summary>
        /// Register generic types.
        /// </summary>
        /// <param name="genericType">The generic type to register</param>
        /// <param name="dataTypes">The data types to be combined with generic type.</param>
        void RegisterGeneric(Type genericType, IEnumerable<Type> dataTypes);
    }
}

#if NET452
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace Nuve.DataStore.Serializer.JsonNet
{
    /// <summary>
    /// Special contract resolver to create objects bypassing constructor call.
    /// </summary>
    public class NoConstructorCreationContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonObjectContract"/> for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// A <see cref="T:Newtonsoft.Json.Serialization.JsonObjectContract"/> for the given type.
        /// </returns>
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            // prepare contract using default resolver
            var objectContract = base.CreateObjectContract(objectType);

            var typeInfo = objectContract.CreatedType.GetTypeInfo();
            // if type has constructor marked with JsonConstructor attribute or can't be instantiated, return default contract
            if (objectContract.OverrideCreator != null || typeInfo.IsInterface || typeInfo.IsAbstract)
                return objectContract;

            // if type has parameterized constructor and any of constructor parameters corresponds to non writable property, return default contract
            // this is needed to handle special cases for types that can be initialized only via constructor, i.e. Tuple<>
            if (objectContract.OverrideCreator != null
                && objectContract.CreatorParameters.Any(parameter =>
                {
                    var propertyForParameter = objectContract.Properties.FirstOrDefault(property => property.PropertyName == parameter.PropertyName);

                    if (propertyForParameter == null)
                        return false;

                    return !propertyForParameter.Writable;
                }))
                return objectContract;

            // override default creation method to create object without constructor call
            objectContract.DefaultCreatorNonPublic = false;
            objectContract.DefaultCreator = () => FormatterServices.GetSafeUninitializedObject(objectContract.CreatedType);

            return objectContract;
        }
    }
}
#endif
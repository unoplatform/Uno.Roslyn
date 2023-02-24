// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
	internal static class TypeParameterSymbolExtensions
	{
		/// <summary>
        	/// Uses reflection to obtain the EffectiveBaseClassNoUseSiteDiagnostics property and its value. 
        	/// http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.CSharp/Symbols/TypeParameterSymbol.cs#275
        	/// </summary>
		private static MethodInfo GetReflectedEffectiveBaseClassMethodInfo(ITypeParameterSymbol typeSymbol)
		{
			// TODO: This reflection might have been broken starting with https://github.com/dotnet/roslyn/pull/39498
			// Consider removing this method if it's not useful.
			return typeSymbol.GetType().GetRuntimeProperties().FirstOrDefault(methodInfo => methodInfo.Name == "EffectiveBaseClassNoUseSiteDiagnostics").GetMethod;
		}

		/// <summary>
		/// Uses reflection to obtain the EffectiveInterfacesNoUseSiteDiagnostics property and its value. 
		/// http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.CSharp/Symbols/TypeParameterSymbol.cs#300
		/// </summary>
		private static MethodInfo GetReflectedEffectiveInterfaceMethodInfo(ITypeParameterSymbol typeSymbol)
		{
			// TODO: This reflection might have been broken starting with https://github.com/dotnet/roslyn/pull/39498
			// Consider removing this method if it's not useful.
			return typeSymbol.GetType().GetRuntimeProperties().FirstOrDefault(methodInfo => methodInfo.Name == "EffectiveInterfacesNoUseSiteDiagnostics").GetMethod;
		}

		/// <summary>
		/// Returns true if two ITypeParameterSymbols (symbols corresponding to a type parameter, eg 'T' in SomeMethod&lt;T&gt;() { T t = ... }) appear to be equivalent, 
		/// meaning that they appear to have matching constraints.
		/// 
		/// Note: this method is currently not watertight, due to difficult cases like self-referential constraints (eg 'where T : IComparable&lt;T&gt;')
		/// </summary>
		/// <param name="current">Symbol to compare</param>
		/// <param name="other">Symbol to compare</param>
		/// <returns>True if the type parameters are equivalent</returns>
		public static bool HasEquivalentConstraintsTo(this ITypeParameterSymbol current, ITypeParameterSymbol other)
		{
			if (current == null || other == null)
			{
				return false;
			}
			if (current.HasReferenceTypeConstraint != other.HasReferenceTypeConstraint
				|| current.HasValueTypeConstraint != other.HasValueTypeConstraint
				|| current.HasConstructorConstraint != other.HasConstructorConstraint)
			{
				return false;
			}
			return current.ConstraintTypes.AreTypeSetsEquivalent(other.ConstraintTypes);
		}

		/// <summary>
		/// Provides all the resolved/effetive interface constraints of the given type parameter symbol. 
		/// <example>
		/// <code>
		/// public Method&lt;T, U, V&gt; where T : U where U : IEnumerable, V where V : IComparable
		/// // U will have the following effective interface constraints: IEnumerable, Icomparable
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="current">The type parameter symbol to resolve obtain the effective interface constraints from</param>
		/// <returns>All the resolved/effective interfaces that constrain the given type parameter symbol</returns>
		public static IEnumerable<INamedTypeSymbol> GetAllEffectiveInterfaceConstraints(this ITypeParameterSymbol current)
		{
			return ((IEnumerable)GetReflectedEffectiveInterfaceMethodInfo(current).Invoke(current, null)).Cast<INamedTypeSymbol>();
		}

		/// <summary>
		/// Attempts to resolve the constraints on the given type parameter symbol and obtain a concrete named type. 
		/// If no such constraint can be resolved into a named type, null is returned
		/// </summary>
		/// <param name="current">The type parameter symbol to resolve as a constrained type</param>
		/// <returns>The effective/constrained named type that has been resolved from the given type parameter symbol's constraint</returns>
		public static INamedTypeSymbol TryGetAsConstrainedType(this ITypeParameterSymbol current)
		{
			var constrainedBaseType = (INamedTypeSymbol)GetReflectedEffectiveBaseClassMethodInfo(current).Invoke(current, null);

			// If the attempt to get the current type parameter symbol as a restrained type gives us a type symbol representing 'object' 
			// or 'ValueType', then this type parameter is unconstrained
			return constrainedBaseType.SpecialType == SpecialType.System_Object || constrainedBaseType.SpecialType == SpecialType.System_ValueType
				? null
				: constrainedBaseType;
		}
	}
}

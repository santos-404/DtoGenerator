namespace DtoGenerator.Tests;

public class DtoSourceGeneratorTests
{
    // -------------------------------------------------------------------------
    // OptOut mode
    // -------------------------------------------------------------------------

    [Fact]
    public void OptOut_IncludesAllProperties()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserDto", Mode = DtoMode.OptOut)]
            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public string Email { get; set; } = "";
            }
            """);

        Assert.False(result.HasErrors);

        var dto = result.GetSource("UserDto.g.cs");
        Assert.Contains("public int Id", dto);
        Assert.Contains("public string Name", dto);
        Assert.Contains("public string Email", dto);
    }

    [Fact]
    public void OptOut_DtoIgnore_Global_ExcludesFromAllDtos()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserListDto", Mode = DtoMode.OptOut)]
            [GenerateDto("UserDetailDto", Mode = DtoMode.OptOut)]
            public class User
            {
                public int Id { get; set; }

                [DtoIgnore]
                public string Password { get; set; } = "";
            }
            """);

        Assert.False(result.HasErrors);

        var listDto    = result.GetSource("UserListDto.g.cs");
        var detailDto  = result.GetSource("UserDetailDto.g.cs");

        Assert.DoesNotContain("Password", listDto);
        Assert.DoesNotContain("Password", detailDto);
        Assert.Contains("public int Id", listDto);
        Assert.Contains("public int Id", detailDto);
    }

    [Fact]
    public void OptOut_DtoIgnore_Scoped_ExcludesFromOneDto()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserListDto", Mode = DtoMode.OptOut)]
            [GenerateDto("UserDetailDto", Mode = DtoMode.OptOut)]
            public class User
            {
                public int Id { get; set; }

                [DtoIgnore("UserListDto")]
                public string Address { get; set; } = "";
            }
            """);

        Assert.False(result.HasErrors);

        var listDto   = result.GetSource("UserListDto.g.cs");
        var detailDto = result.GetSource("UserDetailDto.g.cs");

        Assert.DoesNotContain("Address", listDto);
        Assert.Contains("public string Address", detailDto);
    }

    // -------------------------------------------------------------------------
    // OptIn mode
    // -------------------------------------------------------------------------

    [Fact]
    public void OptIn_ExcludesAllByDefault()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserDto", Mode = DtoMode.OptIn)]
            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
            }
            """);

        Assert.False(result.HasErrors);

        var dto = result.GetSource("UserDto.g.cs");
        Assert.DoesNotContain("public int Id", dto);
        Assert.DoesNotContain("public string Name", dto);
    }

    [Fact]
    public void OptIn_DtoInclude_Scoped_AddsOnlyThatProperty()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserDetailDto", Mode = DtoMode.OptIn)]
            public class User
            {
                public int Id { get; set; }

                [DtoInclude("UserDetailDto")]
                public string Email { get; set; } = "";

                public string Password { get; set; } = "";
            }
            """);

        Assert.False(result.HasErrors);

        var dto = result.GetSource("UserDetailDto.g.cs");
        Assert.Contains("public string Email", dto);
        Assert.DoesNotContain("public int Id", dto);
        Assert.DoesNotContain("Password", dto);
    }

    [Fact]
    public void OptIn_DtoIgnore_OverridesDtoInclude()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserDto", Mode = DtoMode.OptIn)]
            public class User
            {
                [DtoInclude("UserDto")]
                [DtoIgnore("UserDto")]
                public string Email { get; set; } = "";
            }
            """);

        Assert.False(result.HasErrors);

        var dto = result.GetSource("UserDto.g.cs");
        Assert.DoesNotContain("Email", dto);
    }

    // -------------------------------------------------------------------------
    // [DtoName] – C# identifier rename
    // -------------------------------------------------------------------------

    [Fact]
    public void DtoName_Scoped_RenamesCSharpProperty()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserDto", Mode = DtoMode.OptIn)]
            public class User
            {
                [DtoInclude("UserDto")]
                [DtoName("FullName", "UserDto")]
                public string Name { get; set; } = "";
            }
            """);

        Assert.False(result.HasErrors);

        var dto = result.GetSource("UserDto.g.cs");
        Assert.Contains("public string FullName", dto);
        Assert.DoesNotContain("public string Name", dto);
    }

    [Fact]
    public void DtoName_Global_RenamesInAllDtos()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("DtoA", Mode = DtoMode.OptOut)]
            [GenerateDto("DtoB", Mode = DtoMode.OptOut)]
            public class Item
            {
                [DtoName("Label")]
                public string Name { get; set; } = "";
            }
            """);

        Assert.False(result.HasErrors);

        Assert.Contains("public string Label", result.GetSource("DtoA.g.cs"));
        Assert.Contains("public string Label", result.GetSource("DtoB.g.cs"));
    }

    // -------------------------------------------------------------------------
    // [DtoJsonName] – JSON serialization rename
    // -------------------------------------------------------------------------

    [Fact]
    public void DtoJsonName_EmitsJsonPropertyNameAttribute()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserDto", Mode = DtoMode.OptIn)]
            public class User
            {
                [DtoInclude("UserDto")]
                [DtoJsonName("full_name", "UserDto")]
                public string Name { get; set; } = "";
            }
            """);

        Assert.False(result.HasErrors);

        var dto = result.GetSource("UserDto.g.cs");
        Assert.Contains("[JsonPropertyName(\"full_name\")]", dto);
        Assert.Contains("public string Name", dto);   // C# name unchanged
    }

    [Fact]
    public void DtoName_And_DtoJsonName_Combined()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserDto", Mode = DtoMode.OptIn)]
            public class User
            {
                [DtoInclude("UserDto")]
                [DtoName("FullName", "UserDto")]
                [DtoJsonName("full_name", "UserDto")]
                public string Name { get; set; } = "";
            }
            """);

        Assert.False(result.HasErrors);

        var dto = result.GetSource("UserDto.g.cs");
        Assert.Contains("[JsonPropertyName(\"full_name\")]", dto);
        Assert.Contains("public string FullName", dto);    // C# name from [DtoName]
        Assert.DoesNotContain("public string Name", dto);  // original gone
    }

    // -------------------------------------------------------------------------
    // [DtoFlatten]
    // -------------------------------------------------------------------------

    [Fact]
    public void DtoFlatten_FlattensNestedProperties()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            public class Address
            {
                public string Street { get; set; } = "";
                public string City { get; set; } = "";
            }

            [GenerateDto("UserDto", Mode = DtoMode.OptOut)]
            public class User
            {
                public int Id { get; set; }

                [DtoFlatten("UserDto")]
                public Address HomeAddress { get; set; } = new();
            }
            """);

        Assert.False(result.HasErrors);

        var dto = result.GetSource("UserDto.g.cs");
        Assert.Contains("public string Street", dto);
        Assert.Contains("public string City", dto);
        Assert.DoesNotContain("public Address", dto);   // not exposed as-is
    }

    // -------------------------------------------------------------------------
    // [DtoWithMapping]
    // -------------------------------------------------------------------------

    [Fact]
    public void DtoWithMapping_GeneratesPartialMapperWithHook()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserDto", Mode = DtoMode.OptOut)]
            [DtoWithMapping]
            public class User
            {
                public int Id { get; set; }
            }
            """);

        Assert.False(result.HasErrors);

        var mapper = result.GetSource("UserMapper.g.cs");
        Assert.Contains("public static partial class UserMapper", mapper);
        Assert.Contains("static partial void MapCustom(", mapper);
        Assert.Contains("MapCustom(source, dest);", mapper);
    }

    // -------------------------------------------------------------------------
    // Namespace
    // -------------------------------------------------------------------------

    [Fact]
    public void DefaultNamespace_IsSourceNamespacePlusDTOs()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserDto")]
            public class User
            {
                public int Id { get; set; }
            }
            """);

        Assert.False(result.HasErrors);

        var dto = result.GetSource("UserDto.g.cs");
        Assert.Contains("namespace MyApp.Domain.DTOs", dto);
    }

    [Fact]
    public void CustomNamespace_OverridesDefault()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserDto", Namespace = "MyApp.Contracts")]
            public class User
            {
                public int Id { get; set; }
            }
            """);

        Assert.False(result.HasErrors);

        var dto = result.GetSource("UserDto.g.cs");
        Assert.Contains("namespace MyApp.Contracts", dto);
        Assert.DoesNotContain("MyApp.Domain.DTOs", dto);
    }

    // -------------------------------------------------------------------------
    // Mapper generation
    // -------------------------------------------------------------------------

    [Fact]
    public void Mapper_GeneratesExtensionMethodForEachDto()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            [GenerateDto("UserListDto", Mode = DtoMode.OptOut)]
            [GenerateDto("UserDetailDto", Mode = DtoMode.OptOut)]
            public class User
            {
                public int Id { get; set; }
            }
            """);

        Assert.False(result.HasErrors);

        var mapper = result.GetSource("UserMapper.g.cs");
        Assert.Contains("ToUserListDto(this MyApp.Domain.User source)", mapper);
        Assert.Contains("ToUserDetailDto(this MyApp.Domain.User source)", mapper);
    }

    [Fact]
    public void Mapper_FlattenedProperties_MappedFromNestedSource()
    {
        var result = GeneratorTestHelper.Run("""
            using DtoGenerator.Attributes;

            namespace MyApp.Domain;

            public class Address
            {
                public string Street { get; set; } = "";
            }

            [GenerateDto("UserDto", Mode = DtoMode.OptOut)]
            public class User
            {
                [DtoFlatten("UserDto")]
                public Address HomeAddress { get; set; } = new();
            }
            """);

        Assert.False(result.HasErrors);

        var mapper = result.GetSource("UserMapper.g.cs");
        // Flattened property should be mapped via the nested object
        Assert.Contains("Street = source.HomeAddress.Street", mapper);
    }
}

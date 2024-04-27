[![build](https://github.com/ChaseHuegel/needlefish/actions/workflows/build.yml/badge.svg)](https://github.com/ChaseHuegel/needlefish/actions/workflows/build.yml) [![test](https://github.com/ChaseHuegel/needlefish/actions/workflows/test.yml/badge.svg?branch=main)](https://github.com/ChaseHuegel/needlefish/actions/workflows/test.yml) [![publish artifact](https://github.com/ChaseHuegel/needlefish/actions/workflows/publish.yml/badge.svg)](https://github.com/ChaseHuegel/needlefish/actions/workflows/publish.yml)

# Needlefish
Needlefish is a slim and fast binary serialization solution utilizing `.nsd` files to define data models and the `nsdc` compiler to generate highly optimized code for those models and their serialization. The format is intended for performance and memory focused networking, and can be used as a binary storage format.

## NSD Files
The Needlefish Schema Definition ("nsd") is a file that defines a data, or message, model at a high level and is agnostic to both platform or language. This contains the name and fields of a data model which is used by a compiler to generate compatible models in other formats.

## NSDC Compiler
The Needlefish Schema Definition Compiler ("nsdc") is a CLI tool that consumes nsd files to generate code for data models, as well as their serialization and deserialization.

## Limitations & Future Plans
- Nsdc currently only emits C# code. Other languages may become supported if the need comes up.
- Nsdc for C# only emits structs but there are plans to introduce a flag to emit classes.
- Generated C# code has no external dependencies but must be placed into a project that is `netstandard2.1` compatible and allows `unsafe` codeblocks.
- Nsdc will support include declarations in the future.
- Nsdc may support reading and/or writing other data formats in the future (ex: xml, json).
- The format trades compression for speed but it aims to not expand your data. When using nsd for storage, the size on disk will be close to the size in memory and smaller than human-readable formats such as JSON or XML.

# Getting Started
To get started you need a `.nsd` file. The definitions in a `.nsd` file are straightfoward. You add a message for each data model, and specify the type and name for each field within that model.

The `.nsd` file always begins with a version declaration to specify the language version in use. This determines syntax and supported capabilities.

The `.nsd` file also begins with a namespace declaration to prevent conflicts between projects. In C# this is the namespace of the generated code.

```
#version 1.0;
#namespace Samples.Nsd;
```

Next is the type definitions:
- A message contains a set of typed fields and it supports most primitive data types, such as `bool`, `int`, `long` `float`, `double`, and `string`. You can also use other defined types as field types.
- An enum contains a set of predefined constants. These are always unsigned 16 bit numbers.

```
#version 1.0;
#namespace Samples.Nsd;

message Person
{
    int Id;
    string Name;
    string? Email;

    PhoneNumber[]? Phones = 10;
}

enum PhoneType
{
    Unknown;
    Mobile;
    Home = 5;
    Work;
}

message PhoneNumber
{
    string Number;
    PhoneType Type;
}

message AddressBook
{
    Person[] People;
}
```

As in the example above any type can be specified as optional (specified with `?`) and/or an array (specified with `[]`). The `Person` contains an optional `Email` string, and an optional array of `PhoneNumber` messages.

The `= ...;` assignment on field elements in messages specifies the unique ID that field uses for encoding and decoding. This is optional to provide, and if not present will be inferred based on order and the previously defined field. `Person.Id` will have an ID of `0`, `Person.Name` will be `1`, and so on. `Person.Phones` is specifying an ID of `10`, and any implicit element within the message following it would be `11`. This can be a useful mechanism for managing compatibility and versioning of your message definitions.

Enums have field elements similarly to messages, but their type is implicitly ushort and aren't defined in the nsd. The `= ...;` assignment on field elements in enums specifies the constant value of the field. Similarly to the IDs of fields within messages, these values are optional to specify and will be inferred based on order and the previously defined field. `PhoneType.Unknown` will be `0`, `PhoneType.Mobile` is `1`, `PhoneType.Home` is `5`, and `PhoneType.Work` is `6`.

If a field type is an array (specified by `[]`), it can contain a number of values from 0 to a maximum of 65,535. The order of values is preserved by the format.

If a field type is optional (specified by `?`), it may not contain a value at all. Optional fields that don't contain a value aren't encoded at all, so the specifier is particularly good for optimizing your data for size.

## Compiling NSD Files
Once you have `.nsd` files, you need to generate the code to reference, read, and write your data models. You need to run `nsdc` on your `.nsd` file(s).

1. If you haven't installed the compiler, place `nsdc.exe` in your PATH or open a terminal where it is located.

2. Run the compiler, optionally specifying the input (`-i`) path (`$SRC`) and/or output (`-o`) path (`$OUTPUT`) and whether to search for `.nsd` files at the input path recursively (`-r`). If either the input or output path are not provided, each will use the current directory.
    > nsdc -r -i $SRC -o $OUTPUT

This will generate `.cs` source code files in the specified output directory which can be added to a C# project to be compiled.
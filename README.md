# Argon
C# Argon Database Interpreter

## Documentation
Argon treats files as a series of bytes instead of characters. This allows it to use minimal memory to process database information. 

### Processors
A `Processor` object performs a specific action like creating a table while bytes are being read from a file.

The `Processor.callback(byte[] bytes)` method processes the bytes stored by the `register` to modify a table.

The `Processor.empty` property is true when the `Processor` doesn't have a `callback` method. These processors are used as separators and terminators. They do not process any data.

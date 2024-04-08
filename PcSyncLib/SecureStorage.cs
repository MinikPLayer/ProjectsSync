using System.Runtime.InteropServices;
using System.Text;
#pragma warning disable CS0169 // The field is never used

static unsafe class StringUtils {
    public static string CharArrayToString(char* data) {
        var sb = new StringBuilder();
        byte* byteData = (byte*)data;
        while(*byteData != 0) {
            sb.Append((char)*byteData);
            byteData++;
        }

        return sb.ToString();
    }
}

static unsafe class NativeMethods {
    const string LIB_NAME = "libsecret-1";

    public struct SecretSchemaAttribute {
        private char* name;
        private int type;

        public void Dispose()
        {
            if(name != null)
                Marshal.FreeHGlobal((nint)name);

            name = null;
        }

        public void Reset(bool force)
        {
            if(name != null && !force)
                Dispose();

            name = null;
            type = 0;
        }


        public SecretSchemaAttribute(string name, int type) {
            this.name = (char*)Marshal.StringToHGlobalAnsi(name);
            this.type = type;
        }

        public SecretSchemaAttribute()
        {
            Reset(true);
        }
    }

    public struct SecretSchema : IDisposable {
        const int ATTRIBUTES_LENGTH = 32;

        char* name; 
        int flags;
        SecretSchemaAttribute* attributes;

        private readonly int _reserved;
        private readonly void* _reserved_1;
        private readonly void* reserved2;
        private readonly void* reserved3;
        private readonly void* reserved4;
        private readonly void* reserved5;
        private readonly void* reserved6;
        private readonly void* reserved7;

        public void SetName(string name) {
            if(this.name != null)
                Marshal.FreeHGlobal((IntPtr)this.name);

            this.name = (char*)Marshal.StringToHGlobalAnsi(name);
        }

        public string GetName() {
            return StringUtils.CharArrayToString(name);
        }

        public void AddAttribute(int i, SecretSchemaAttribute attribute) {
            attributes[i].Dispose();
            attributes[i] = attribute;
        }

        public void Dispose()
        {
            if(name != null)
            {
                Marshal.FreeHGlobal((IntPtr)name);
                name = null;
            }
                
            for(var i = 0; i < ATTRIBUTES_LENGTH; i++)
                attributes[i].Dispose();

            Marshal.FreeHGlobal((IntPtr)attributes);
        }

        public SecretSchema(SecureStorage.SecretSchema managedSchema) : this() {
            SetName(managedSchema.Name);
        }

        public SecretSchema() 
        {
            name = null;
            flags = 0; // SECRET_SCHEMA_NONE
            attributes = (SecretSchemaAttribute*)Marshal.AllocHGlobal(ATTRIBUTES_LENGTH * sizeof(SecretSchemaAttribute));
            for(var i = 0; i < ATTRIBUTES_LENGTH; i++)
                attributes[i].Reset(true);    
        }
    }

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool secret_password_store_sync(SecretSchema* schema, char* collection, char* label, char* password, IntPtr gcancellable, IntPtr *error, IntPtr va_arg);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern char* secret_password_lookup_sync(SecretSchema* schema, IntPtr gcancellable, IntPtr *error);

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void secret_password_free(char* password);
}

public static class SecureStorage {
    public class SecretSchemaAttribute {
        public enum Types {
            String = 0,
            Integer = 1,
            Boolean = 2,
        }

        public string Name {get;set;}
        public Types Type {get;set;}
        public object Value {get;set;}

        internal NativeMethods.SecretSchemaAttribute ToNative() {
            return new NativeMethods.SecretSchemaAttribute(Name, (int)Type);
        }

        public SecretSchemaAttribute(string name, string value) {
            Name = name;
            Type = Types.String;
            Value = value;
        }
        
        public SecretSchemaAttribute(string name, int value) {
            Name = name;
            Type = Types.Integer;
            Value = value;
        }

        public SecretSchemaAttribute(string name, bool value) {
            Name = name;
            Type = Types.Boolean;
            Value = value;
        }
    }

    public class SecretSchema {
        public string Name {get;set;}
        public SecretSchemaAttribute[] Attributes {get;set;}

        internal NativeMethods.SecretSchema ToNative()
        {
            var native = new NativeMethods.SecretSchema();
            native.SetName(this.Name);

            for(var i = 0; i < Attributes.Length; i++) {
                native.AddAttribute(i, Attributes[i].ToNative());
            }

            return native;
        }

        public SecretSchema(string name, params SecretSchemaAttribute[] attributes) {
            if(attributes.Length > 32) {
                throw new ArgumentException("Too many attributes. Maximum is 32.");
            }

            Name = name;
            Attributes = attributes;
        }
    }

    public static unsafe string? GetPassword(SecretSchema schema) {
        IntPtr error = IntPtr.Zero;
        using var nativeSchema = schema.ToNative();

        var password = NativeMethods.secret_password_lookup_sync(&nativeSchema, IntPtr.Zero, &error);
        if(error != IntPtr.Zero) {
            throw new InvalidOperationException("Failed to get password - error.");
        }
        else if(password == null) {
            // TODO: Handle error
            throw new InvalidOperationException("Failed to find password.");
        }

        var retStr = StringUtils.CharArrayToString(password);
        NativeMethods.secret_password_free(password);
        return retStr;
    }
}
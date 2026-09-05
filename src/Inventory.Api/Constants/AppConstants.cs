namespace Inventory.Api.Constants;

public static class AppConstants
{
    public static class ErrorMessages
    {
        public const string Unauthorized = "No autorizado";
        public const string Forbidden = "Acceso denegado";
        public const string NotFound = "Recurso no encontrado";
        public const string ServerError = "Error interno del servidor";
        public const string ValidationError = "Error de validación";
        public const string UserNotFound = "Usuario no encontrado";
        public const string ProductNotFound = "Producto no encontrado";
        public const string CategoryNotFound = "Categoría no encontrada";
        public const string InsufficientStock = "Stock insuficiente";
        public const string InvalidSale = "La venta debe contener al menos un producto válido";
        public const string InvalidCredentials = "Credenciales inválidas";
        public const string PasswordTooWeak = "La contraseña debe tener al menos 8 caracteres e incluir mayúscula, minúscula y un número";
        public const string InvalidUsername = "El nombre de usuario solo puede contener letras, números y guion bajo (3-50 caracteres)";
        public const string UsernameTaken = "El nombre de usuario ya está en uso";
        public const string EmailTaken = "El email ya está registrado";
        public const string InvalidSku = "El SKU solo puede contener letras, números, guiones y guion bajo (3-50 caracteres)";
        public const string SkuTaken = "Ya existe un producto con ese SKU";
    }

    public static class SuccessMessages
    {
        public const string OperationCompleted = "Operación completada exitosamente";
        public const string UserRegistered = "Usuario registrado exitosamente";
        public const string LoginSuccessful = "Login exitoso";
        public const string ProductCreated = "Producto creado exitosamente";
        public const string ProductUpdated = "Producto actualizado exitosamente";
        public const string ProductDeleted = "Producto eliminado exitosamente";
        public const string CategoryCreated = "Categoría creada exitosamente";
        public const string CategoryUpdated = "Categoría actualizada exitosamente";
        public const string CategoryDeleted = "Categoría eliminada exitosamente";
        public const string SaleRegistered = "Venta registrada exitosamente";
        public const string PurchaseRegistered = "Compra registrada exitosamente";
    }

    public static class ValidationLimits
    {
        public const int UsernameMinLength = 3;
        public const int UsernameMaxLength = 50;
        public const int PasswordMinLength = 8;
        public const int EmailMaxLength = 256;
        public const int ProductNameMinLength = 3;
        public const int ProductNameMaxLength = 200;
        public const int ProductSkuMinLength = 3;
        public const int ProductSkuMaxLength = 50;
        public const int CategoryNameMinLength = 3;
        public const int CategoryNameMaxLength = 100;
        public const int MaxStock = 1000000;
        public const double MinPrice = 0.01;
        public const double MaxPrice = 999999.99;
    }

    public static class DefaultValues
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
        public const int JwtExpirationMinutes = 60;
    }

    public static class CorsPolicies
    {
        public const string Default = "InventoryApiCors";
    }
}

# Changelog

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.0.9776] - 2026-10-06

### Fixed
- **Facturación Multimoneda y Validación AFIP WSFE:**
  - Conversión precisa de artículos expresados en moneda extranjera (USD) a pesos argentinos (ARS) en la emisión de comprobantes fiscales electrónicos.
  - Reemplazo de truncamiento (`Currency.Truncate`) por redondeo simétrico (`MidpointRounding.AwayFromZero`) en totales, PVP, costos, líneas de detalle y alícuotas, eliminando discrepancias por centavos.
  - Proporcionalidad exacta en bases imponibles e importes de IVA aplicando el factor de descuento global del comprobante.
  - Reconciliación matemática en `ServicioFacturaElectronica`: derivación directa de `ImpNeto`, `ImpIVA` e `ImpTotal` a partir de las alícuotas validadas (`AlicIva[]`), garantizando estricta concordancia con la tolerancia oficial de AFIP (+/- 0.01) y eliminando los errores 10014, 10015 y 10016.
  - Corrección de doble conversión de moneda en artículos tipo receta (`EsReceta`).
- **Comprobantes Mixtos con Servicios (Concepto 3 AFIP):**
  - Corrección del operador de bits (`&` en lugar de `|`) en `ClienteAfipWsfe` y `ServicioFacturaElectronica`, permitiendo que facturas con productos y servicios reconozcan y envíen las fechas obligatorias.
  - Agregado del valor `ProductosYServicios = 3` al enumerador `Conceptos`.
  - Búsqueda específica de renglones de servicio para cálculo de periodicidad y corrección de operaciones inmutables en `DateTime`.
  - Fallback automático a la fecha actual para `FchServDesde`, `FchServHasta` y `FchVtoPago`, evitando el error de AFIP "El campo fecha desde y hasta no está especificado".
  - Asignación por defecto de Concepto 1 (Productos) en comprobantes compuestos exclusivamente por ítems con texto libre (`*`).
- **Modificador Masivo de Precios (`CambioMasivoPrecios`):**
  - Corrección de índices de columnas en el listado de artículos y en la grilla de actualización masiva.
  - Soporte multimoneda en la modificación de precios, respetando la moneda de origen de cada artículo y aplicando la cotización adecuada.
- **Generador de Facturas en PDF (`GeneradorPdf`):**
  - Precios unitarios formateados con 2 decimales finales.
  - En Facturas B y C, el subtotal se presenta con IVA incluido para concordancia visual con la suma de los renglones, ocultando alícuotas en cero sin valor fiscal.

## [2.0.9775] - 2026-10-06

### Added
- **Adecuación RG 5.616 (AFIP / ARCA WSFEv1):**
  - Incorporación del elemento `<CondicionIVAReceptorId>` en el contrato WSDL (`service1.wsdl`) y en el contrato de datos WCF `FEDetRequest` (`Reference.cs`) con `Order = 17`.
  - Propiedad `CondicionIvaReceptorId` en la entidad `Afip.Ws.FacturaElectronica.Cliente`.
  - Método `MapearCondicionIvaReceptor(Persona cliente)` en `ClienteAfipWsfe.cs` para traducir situaciones tributarias locales (Consumidor Final, Resp. Inscripto, Monotributo, Exento, etc.) a códigos oficiales de AFIP (1, 4, 5, 6, 7, 15).
  - Inyección automática del código de condición fiscal del receptor en cada comprobante del lote en `FECAEDetRequest`.
- **Entorno de Homologación Dinámico:**
  - Control visual en Preferencias: Casilla de verificación `CheckAfipHomologacion` en la solapa "Comprobantes" para alternar fácilmente entre entorno oficial y homologación con un solo clic y retroalimentación de estado en tiempo real.
  - Propiedad estática `Config.AfipHomologacion` con persistencia en la tabla `sys_config` (`AFIP.Homologacion`).
  - Soporte de conmutación sin cambios en base de datos mediante variables de entorno `LAZARO_AFIP_HOMOLOGACION=1` o `AFIP_HOMO=1`.
  - Factoría de clientes WCF `CrearClienteSoap()` y resolución dinámica de endpoints en `ServicioFacturaElectronica` y `ServicioAutenticacion` (WSAA).
- **Coexistencia de Certificados Digitales:**
  - Búsqueda inteligente de certificados: `Certificado_homo.p12` en homologación y `Certificado_prod.p12` / `Certificado.p12` en producción. Permite tener ambos certificados en la carpeta `AFIP` del negocio sin rotación manual.
- **Modo Debug / Volcado XML:**
  - Detección automática de depuración en Visual Studio: `System.Diagnostics.Debugger.IsAttached` activa el modo debug sin requerir configuración manual.
  - Método `VolcarXmlDebug()` en `ServicioFacturaElectronica` para formatear y volcar los payloads SOAP salientes y entrantes a consola y a archivos de texto en `%TEMP%\afip_fecaesolicitar_*.xml` bajo demanda (`LAZARO_AFIP_DEBUG=1`, depuración en VS o `ServicioFacturaElectronica.ModoDebug = true`).
- **Salvaguardas de Seguridad para Homologación:**
  - Bloqueo preventivo por colisión numérica inminente contra la base de datos local en `ComprobanteController.ImprimirFacturaElectronicaAfip`.
  - Detección de Punto de Venta productivo para advertir y frenar emisiones accidentales de prueba en puntos de venta reales.
  - Marcado indeleble en base de datos (`comprob.obs`) con la leyenda `[AFIP HOMOLOGACIÓN - COMPROBANTE NO FISCAL]`.
  - Franja superior roja `"COMPROBANTE NO FISCAL - MODO HOMOLOGACIÓN AFIP"` y pie de página distintivo en los PDFs generados con `GeneradorPdf`.

### Changed
- `ServicioAutenticacion` y `ServicioFacturaElectronica` ahora soportan URLs configurables en tiempo de ejecución.
- Descentralización del almacenamiento del Ticket de Acceso (TA): se guarda en la carpeta aislada de cada empresa (`Documents\Lázaro\<Empresa>\AFIP\ticketacceso_<modo>.dat`).
- Claves de BD para el TA separadas por entorno (`AFIP.TicketAcceso.Prod` y `AFIP.TicketAcceso.Homo`), manteniendo compatibilidad retroactiva de lectura/escritura con `AFIP.TicketAcceso`.
- Se añadió referencia a `netstandard` en `Lbl.csproj` para asegurar la compatibilidad con librerías modernas de QR y códigos de barras en .NET Framework 4.8.

### Fixed
- **Colisión de Ticket de Acceso Multi-Instancia:** Se eliminó la sobreescritura del archivo temporal global en `%TEMP%` cuando coexisten múltiples instancias de Lázaro para distintos negocios en una misma máquina.
- **Validación de CUIT en TA:** Se incorporó el CUIT como 5to parámetro en la cadena serializada del ticket para validar la pertenencia antes de reutilizarlo.
- **Compatibilidad de Compilación:** Corrección de construcciones de sintaxis para compatibilidad con compiladores legados (.NET 4.0 / C# 5) y modernos (Roslyn C#).

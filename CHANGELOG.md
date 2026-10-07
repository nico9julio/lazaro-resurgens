# Changelog

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

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

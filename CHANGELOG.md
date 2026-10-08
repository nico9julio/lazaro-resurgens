# Changelog

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.9782] - 2026-10-08

### Added
- **Monotributo — Detección Automática de Discrepancias y Cuadro Comparativo Antes/Después:**
  - **Coordinador Asíncrono en Segundo Plano (`VerificadorActualizacionesMonotributo`):** Verificación no bloqueante en hilo secundario de las escalas oficiales publicadas por ARCA / AFIP sin demorar el inicio del sistema ni congelar la pantalla.
  - **Doble Disparador Inteligente:** Comprobación diferida tras iniciar la aplicación (`Inicio.cs`) y al abrir el análisis de detalle (`FormDetalleMonotributo.cs`), así como rechequeo forzado con el botón *Actualizar Datos*.
  - **Comparación Inteligente de Topes y Cuotas (`DetectarDiscrepancias`):** Identificación precisa de diferencias de importes entre la base de datos de la empresa y la publicación oficial de AFIP, generando un cuadro comparativo alineado con variación porcentual (*Antes ➔ Nuevo oficial AFIP*).
  - **Confirmación con Memoria de Sesión:** Cuadro de diálogo modal que solicita aprobación al usuario antes de modificar la base de datos; si el usuario desestima la actualización, se silencia en la sesión para evitar interrupciones reiteradas.
- **Monotributo — Consulta Oficial de Categoría Registrada en AFIP:**
  - Integración con el Web Service oficial de Constancia de Inscripción de ARCA / AFIP (Persona Service A5) para detectar automáticamente la categoría inscripta real mediante certificado digital fiscal.
- **Monotributo — Badges Gráficos Compactos y Oportunidad de Baja:**
  - Rediseño de los indicadores de la barra inferior y ventana de detalle con estructura compacta de 3 niveles: ícono + título, barra de consumo con porcentaje centrado en su interior y saldo restante.
  - Badge dedicado para oportunidad de recategorización a la baja ("Puedes facturar hasta..." / "Ahorrarías").

### Fixed
- **Monotributo — Corrección de Cálculo de Exceso ("SUPERADO por $ 0"):**
  - Se eliminó el truncamiento que forzaba a cero el margen de la categoría inscripta al ser superada, calculando el importe y porcentaje real de exceso.
- **Monotributo — Actualización de Topes Predeterminados Oficiales:**
  - Escalas base actualizadas a los valores oficiales vigentes de ARCA / AFIP (Categoría D: $ 30.628.651,43; etc.).
- **Interfaz — Solapamiento Visual en Banner:**
  - Desacople del ícono institucional en un control independiente dentro del banner para evitar solapamiento sobre el texto.

## [2.0.9781] - 2026-10-07

### Added
- **Monotributo — Sincronización Manual Directa desde el Portal Oficial de ARCA / AFIP:**
  - **Parser Web Nativo (`ParserWebAfip`):** Consulta y parseo automático de la tabla oficial publicada en `https://www.afip.gob.ar/monotributo/categorias.asp` (con fallback de dominio hacia `arca.gob.ar`), sin requerir registro de cuenta ni API Keys de terceros.
  - **Extracción Integral de Escalas y Cuotas:** Obtiene para las 11 categorías (de la A a la K) los ingresos brutos máximos anuales vigentes, la cuota mensual de prestaciones de servicios y la cuota mensual de venta de cosas muebles (comercio), junto con la leyenda oficial de vigencia emitida por el organismo fiscal.
  - **Sincronización Estrictamente Manual:** La consulta web se ejecuta exclusivamente a demanda del usuario desde la ventana de configuración mediante el botón `[🌐 Sincronizar desde Web Oficial AFIP]`, asegurando que no se ejecuten peticiones automáticas en segundo plano ni al iniciar la aplicación.
  - **Interfaz de Configuración Mejorada:** Rediseño del panel superior de `FormConfigurarEscalasMonotributo` con acceso prioritario a la sincronización oficial directa de AFIP en un clic, conservando la integración alternativa con Servidos Tax API y carga de URL personalizada como métodos de respaldo.

## [2.0.9780] - 2026-10-07

### Added
- **Monotributo — Widget de Facturación, Proyección de Recategorización y Sincronización con ARCA:**
  - **Widget en Barra Inferior:** Indicador informativo en la barra de estado inferior que se activa automáticamente cuando la empresa configurada tiene la Condición IVA *Responsable Monotributista* (código `4`).
  - **Monitoreo y Métricas en Tiempo Real:** Visualización en vivo del total facturado en el mes en curso (MTD), total del mes anterior, acumulado móvil de los últimos 12 meses y cálculo de facturación proyectada anualizada para la próxima recategorización semestral según el calendario oficial de ARCA (períodos de recategorización de enero y julio).
  - **Badge de Categoría con Progreso Visual:** Indicador estilizado que muestra la categoría actual y proyectada con relleno porcentual progresivo sobre el tramo de facturación consumido, cálculo de monto restante para el tope de la categoría o siguiente categoría, advertencia cromática (alerta naranja en Cat. K y roja ante riesgo de exclusión hacia el Régimen General).
  - **Personalización de Métricas:** Menú contextual accesible mediante clic derecho sobre el widget que permite al usuario seleccionar qué métricas desea visualizar u ocultar en la barra inferior (mes actual, mes anterior, acumulado 12 meses, facturación proyectada).
  - **Ventana de Detalle y Proyección:** Modal accesible con doble clic en el widget o clic en el badge, que detalla tarjetas resumen de estado, proyección semestral/anual, historial mes a mes con desglose de comprobantes y grilla completa de escalas oficiales de ARCA con topes y cuotas.
  - **Integración con Tax API de Servidos Developers (servidos.ar):** Asistente para configurar API Key gratuita o personalizada y descargar en un clic las escalas oficiales vigentes publicadas por ARCA, con soporte para actividades de servicios y comercio.
  - **Formateo Monetario Unificado:** Representación de todos los importes del control de monotributo en formato moneda sin centavos con punto para separación de miles (ej. `$ 1.234.568`).

## [2.0.9779] - 2026-10-07

### Fixed
- **Detalle de Comprobantes — Recálculo Inmediato ante Modificación Manual de Precios y Cantidades:**
  - **Problema corregido:** Al editar manualmente el precio unitario, la cantidad o el porcentaje de descuento en un renglón de comprobante (o al ingresar ítems manuales libres que comienzan con asterisco `*`), el campo "Importe" del renglón quedaba congelado en su valor anterior (en $0,00 o en el PVP de base de datos). Como consecuencia directa, el evento `ImportesChanged` no se propagaba a la grilla y los campos Subtotal y Total del formulario no reflejaban las modificaciones manuales introducidas por el usuario.
  - **Causa raíz:** En la versión 2.0.9778, la bandera de control de reentrancia (`m_Recalculando`) se activaba en `EntradaUnitarioIvaDescuentoCantidad_TextChanged` antes de invocar `RecalcularImporteFinal()`, provocando que dicho método abortara en su primera línea y nunca calculara el importe final ni disparara la actualización de totales del formulario.
  - **Solución e implicancia:** Se reubicó la activación de la guarda `m_Recalculando` exclusivamente en el cuerpo de `RecalcularImporteFinal()`. La edición de precios manuales, cantidades, descuentos/recargos y artículos libres con asterisco (`*`) recalcula en tiempo real el importe del renglón y actualiza de inmediato el Subtotal y el Total general del comprobante, manteniendo al mismo tiempo la prevención contra desbordamiento de pila (`StackOverflowException`) en comprobantes de compra.

## [2.0.9778] - 2026-10-07

### Fixed
- **Comprobantes de Compra — Corrección de Recursión Infinita al Crear:**
  - **Problema corregido:** Al intentar abrir una nueva factura de compra (*Comprobantes -> Compras -> Nueva factura de compra*), el programa se cerraba abruptamente sin mostrar ningún mensaje de error. Esto ocurría porque el formulario activa la discriminación de IVA (`DiscriminarIva = true`) para compras. En el control de renglón de comprobante, el recálculo asignaba el importe de IVA unitario a `0`, lo que disparaba el evento de cambio de texto, y este a su vez volvía a invocar el recálculo sin validar cambios ni contar con guarda de reentrancia, produciendo un desbordamiento de pila (`StackOverflowException`).
  - **Solución e implicancia:** Se implementó una bandera de guarda de reentrancia (`m_Recalculando`) y validaciones de desigualdad antes de asignar importes y emitir eventos. La creación de facturas de compra y cualquier comprobante que discrimine IVA carga e interactúa de manera completamente estable.

- **Listados con Filtros por Subcomando — Despachador de Menú:**
  - **Problema corregido:** En opciones como *Comprobantes -> Compras -> Listado de facturas de compra* (comando `LISTAR Lbl.Comprobantes.ComprobanteDeCompra FP`), o listados filtrados por letra de factura (`FA`, `FB`) o tipo de pedido (`NP`, `PD`, `RP`), la grilla se abría completamente vacía sin registros. El despachador `ExecListar` pasaba por error el nombre de la clase LBL en lugar del filtro extraído (`comando`).
  - **Solución e implicancia:** Se corrigió el argumento pasado a `InstanciarFormularioListado`, aplicando correctamente los filtros de compra (`FP`, `NP`, `PD`, `RP`), facturas (`A`, `B`) y artículos a pedir/pedidos.

- **Pruebas Unitarias — Compatibilidad con NUnit 4 (ClassicAssert):**
  - **Problema corregido:** La compilación de la solución completa arrojaba 68 errores en el proyecto `Lbl.Test` debido al cambio de API introducido en NUnit 4.
  - **Solución e implicancia:** Se migraron las aserciones a `NUnit.Framework.Legacy.ClassicAssert`, permitiendo compilar la solución entera (`Lazaro.sln`) con cero errores.

## [2.0.9777] - 2026-10-07

### Fixed
- **Factura C y Régimen Monotributo — Blindaje ante el IVA (AFIP WSFE):**
  - **Problema corregido:** En una Factura C emitida por un contribuyente Monotributista nunca debe calcularse ni discriminarse IVA. Sin embargo, al facturar a un Consumidor Final (o a otro cliente no exento), la interfaz evaluaba la propiedad interna `AplicaIva = true`. Al incorporar un artículo en moneda extranjera (USD) que tenía configurada una alícuota en su ficha (por ejemplo, 21%), el sistema le recargaba automáticamente el 21% de IVA al precio unitario o, si el usuario corregía el precio en la grilla, intentaba desglosarlo dividiendo por 1.21 para calcular una base neta ficticia.
  - **Ejemplo:** Un artículo costaba US$ 10 a una cotización de $1.000 (total esperado: $10.000). El sistema le sumaba un 21% ficticio convirtiéndolo a $12.100. Si el usuario corregía el total manualmente a $10.000, el sistema le quitaba el 21% para calcular el "Neto" ($8.264,46). Al enviar la Factura C a AFIP, el webservice WSFE exige estrictamente la igualdad `ImpTotal == ImpNeto` e `ImpIVA == 0`. Al recibir un importe neto inferior al total, AFIP rechazaba el comprobante por inconsistencia de importes.
  - **Solución e implicancia:** En comprobantes tipo C, `AplicaIva` y `DiscriminarIva` quedan forzados permanentemente a `false`. `SubtotalSinIva` e `ImporteSinIvaFinal` devuelven el total íntegro del comprobante y el IVA es incondicionalmente $0,00. Todo valor ingresado es precio final directo y AFIP aprueba el comprobante sin errores.

- **Facturación Mixta Multimoneda — Residuos Flotantes y Truncamiento:**
  - **Problema corregido:** Los artículos en pesos tienen precios con centavos fijos (ej. $1.500,00), mientras que un artículo en dólares multiplicado por una cotización produce números decimales flotantes extendidos (ej. US$ 12,35 × $1.150,25 = $14.205,5875). Anteriormente, la cotización no se redondeaba de inmediato a 2 decimales y la grilla de la interfaz utilizaba truncamiento hacia abajo (`Currency.Truncate`), mientras que el backend enviaba decimales completos o calculaba con otra precisión.
  - **Ejemplo:** Al combinar en una misma factura un artículo en pesos de $1.000,00 con un artículo en dólares convertido a $1.396,7265:
    - En pantalla la grilla truncaba hacia abajo a $1.396,72, mostrando un subtotal visible de $2.396,72.
    - El backend o el servicio de AFIP redondeaba matemáticamente hacia arriba a $2.396,73.
    - La discrepancia de $0,01 hacía que AFIP rechazara la factura o que el total impreso en el PDF difiriera de la suma visual de los renglones.
  - **Solución e implicancia:** El precio convertido (`PvpLocal`) se redondea de inmediato con redondeo simétrico bancario a 2 decimales (`MidpointRounding.AwayFromZero`). Tanto los artículos en pesos como los artículos en dólares operan exactamente sobre la misma base monetaria estándar, eliminando saltos de centavos al mezclar monedas.

- **Descuentos y Recargos — Unificación del Orden de Redondeo:**
  - **Problema corregido:** Existía disparidad en la fórmula matemática aplicada al calcular descuentos por artículo: la grilla de la interfaz calculaba `Round(Precio × Cantidad × (1 - Descuento))`, mientras que el modelo de datos de negocio y el generador de PDF calculaban `Round(Precio × (1 - Descuento)) × Cantidad`. Además, al aplicar descuentos globales sobre el comprobante en Factura C, el recálculo proporcional inverso podía diferir en centavos con respecto a la resta directa.
  - **Ejemplo:** Al vender 3 unidades de un producto de $105,50 con 10% de descuento:
    - Según el orden de operaciones, el redondeo podía arrojar $284,84 en un componente y $284,85 en otro.
    - En descuentos globales, si el subtotal era de $10.000 y se aplicaba un descuento para que el total quede en $9.000, el recálculo inverso podía derivar un descuento de $999,99 o $1.000,01.
  - **Solución e implicancia:** Se unificó el orden de cálculo en toda la aplicación (grilla de entrada, modelo de datos y PDF): primero se calcula y redondea el precio unitario final con descuento a 2 decimales y luego se multiplica por la cantidad. En Factura C, el importe de descuento/recargo global se calcula restando directamente `Subtotal - Total`, garantizando que la suma visual en pantalla, el comprobante impreso en PDF y el lote enviado a AFIP coincidan con exactitud matemática al centavo.

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

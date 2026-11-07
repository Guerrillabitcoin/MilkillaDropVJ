# CLAUDE.md — Milkilla Drop VJ

@AGENTS.md

Lo técnico de Milkilla Drop VJ está en `AGENTS.md` (importado arriba; si aún no está en esta rama, mandan el README y las decisiones del dueño). Lo común a todos los agentes está en el ACUERDO: tablero crosswalk `guerrillaagents`, archivo `files/ACUERDO.md`. Una decisión posterior del dueño manda sobre todo. Este archivo solo añade lo que Claude (Claude Code o claude.ai) necesita para trabajar aquí sin que se lo repitan en cada sesión.

## Antes de empezar

- `git fetch` y mira el tablero `guerrillaagents` (sección `milkilla`). Si otro agente tiene una tarea abierta en la misma zona, no la toques.
- Una tarea, un objetivo, una rama.

## Tu papel

- En cada tarea, o implementas o revisas: nunca las dos cosas en la misma tarea.
- El dueño técnico es el bot Milkilla Drop VJ, que implementa la fase 1. Motor decidido por el dueño: BeatDrop externo + Spout. No se reabre.

## Ramas y commits

- Una rama por fase o hito: `claude/<hito>`. Nunca push a `main`, nunca `force-push`, no borres ramas ni etiquetas.
- Autor, antes del primer commit:

  ```
  git config user.name "GUERRILLA"
  git config user.email "115675632+Guerrillabitcoin@users.noreply.github.com"
  ```

- No subas `.github/workflows`: déjalo en `docs/ci/` y avisa al dueño.
- Nunca secretos ni claves. Sin nombre real ni email personal. Todo en español.

## Pruebas

- `dotnet build` cuando haya código (necesita Windows o el SDK de .NET 8 con el paquete de escritorio). Si no puedes compilar, dilo.
- BeatDrop, Spout y el MIDI de verdad los prueba el dueño en su PC: déjale los pasos exactos.

## Al terminar

Traspaso en el tablero con el formato del ACUERDO (§3): Qué, Rama @ commit, Probado, Archivos, Riesgos / pendiente, Prueba manual para el dueño, Revisa.

`.claude/settings.json` bloquea el push a `main`, el `force-push`, borrar ramas o etiquetas, editar `.github/workflows` y leer claves. Es una red de seguridad: no sustituye a estas reglas.

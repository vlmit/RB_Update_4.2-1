const viewsWithoutDelete = new Set<string>(['KrDocStateCards']);

export function canDeleteCardFromViewDefault(viewAlias: string): boolean {
  return !viewsWithoutDelete.has(viewAlias);
}

const viewsWithoutExport = new Set<string>(['KrDocStateCards']);

export function canExportCardFromViewDefault(viewAlias: string): boolean {
  return !viewsWithoutExport.has(viewAlias);
}

const viewsWithoutViewStorage = new Set<string>(['KrDocStateCards']);

export function canViewCardStorageFromViewDefault(viewAlias: string): boolean {
  return !viewsWithoutViewStorage.has(viewAlias);
}

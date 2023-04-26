function $InternalCheckExists(s, v, n) {
    if (v === undefined) throw new Error(`Line ${n}: NameError: Cannot reference variable '${s}' before assignment`);
}
The particle system prefabs in this folder serve as the foundation for every effect in this package.

**DO NOT CHANGE** this prefab unless you fully understand its structure, as changes can affect all effects that reference it.


All effect prefabs use these roots as their source which keeps individual prefab files compact.

A full particle system component takes around 115kb and this grows fast when an effect contains several systems or multiple variations. Using a shared root keeps the project lightweight and improves overall maintainability.


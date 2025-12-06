// A simple helper to manage the database connection.
let db;

// Function to open the database. We wrap it in a Promise.
export function openDb(dbName, version) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, version);

        request.onupgradeneeded = event => {
            db = event.target.result;
            // Create object stores if they don't exist
            if (!db.objectStoreNames.contains('conversations')) {
                db.createObjectStore('conversations', { keyPath: 'id' });
            }
        };

        request.onsuccess = event => {
            db = event.target.result;
            resolve('Database opened successfully');
        };

        request.onerror = event => {
            console.error('IndexedDB error:', event.target.error);
            reject('Error opening database');
        };
    });
}

// Function to add a record to a store.
export function addRecord(storeName, record) {
    return new Promise((resolve, reject) => {
        if (!db) {
            reject('Database is not open.');
            return;
        }
        const transaction = db.transaction([storeName], 'readwrite');
        const store = transaction.objectStore(storeName);
        const request = store.put(record); // Use put instead of add to allow updates

        request.onsuccess = () => {
            resolve('Record added successfully');
        };

        request.onerror = event => {
            console.error('Error adding record:', event.target.error);
            reject('Failed to add record');
        };
    });
}

// Function to get a record by its key.
export function getRecord(storeName, key) {
    return new Promise((resolve, reject) => {
        if (!db) {
            reject('Database is not open.');
            return;
        }
        const transaction = db.transaction([storeName], 'readonly');
        const store = transaction.objectStore(storeName);
        const request = store.get(key);

        request.onsuccess = event => {
            // event.target.result will be the record object or undefined.
            resolve(event.target.result);
        };

        request.onerror = event => {
            console.error('Error getting record:', event.target.error);
            reject('Failed to get record');
        };
    });
}

// Function to get all records from a store.
export function getRecords(storeName) {
    return new Promise((resolve, reject) => {
        if (!db) {
            reject('Database is not open.');
            return;
        }
        const transaction = db.transaction([storeName], 'readonly');
        const store = transaction.objectStore(storeName);
        const request = store.getAll();

        request.onsuccess = event => {
            resolve(event.target.result || []);
        };

        request.onerror = event => {
            console.error('Error getting all records:', event.target.error);
            reject('Failed to get all records');
        };
    });
}

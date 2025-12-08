/**
 * IndexedDB Manager for Blazor WebAssembly
 * Provides thread-safe, high-performance database operations
 */

// Database connection management
let db = null;
let dbPromise = null;
const DB_NAME = 'MicroChatDb';
const DB_VERSION = 1;
const CONNECTION_TIMEOUT = 10000; // 10 seconds

/**
 * Ensures the database is open before any operation.
 * Thread-safe: multiple concurrent calls will reuse the same promise.
 * @returns {Promise<IDBDatabase>} The opened database instance
 */
function ensureDbOpen() {
    // Fast path: return existing valid connection
    if (db && !db.closePending && db.objectStoreNames.length > 0) {
        return Promise.resolve(db);
    }

    // Connection in progress: wait for it
    if (dbPromise) {
        return dbPromise;
    }

    // Start new connection with timeout
    dbPromise = Promise.race([
        openDatabase(),
        createTimeout(CONNECTION_TIMEOUT, 'Database connection timeout')
    ]).catch(error => {
        // Reset on failure to allow retry
        db = null;
        dbPromise = null;
        throw error;
    });

    return dbPromise;
}

/**
 * Opens the IndexedDB database
 * @returns {Promise<IDBDatabase>}
 */
function openDatabase() {
    return new Promise((resolve, reject) => {
        // Check if IndexedDB is available
        if (!window.indexedDB) {
            reject(new Error('IndexedDB is not supported in this browser'));
            return;
        }

        const request = indexedDB.open(DB_NAME, DB_VERSION);

        request.onupgradeneeded = event => {
            const database = event.target.result;
            
            // Create object stores if they don't exist
            if (!database.objectStoreNames.contains('conversations')) {
                database.createObjectStore('conversations', { keyPath: 'id' });
            }
            
            // Add indexes here if needed in the future
            // const store = database.objectStoreNames.contains('conversations') 
            //     ? event.target.transaction.objectStore('conversations') : null;
            // if (store && !store.indexNames.contains('timestamp')) {
            //     store.createIndex('timestamp', 'timestamp', { unique: false });
            // }
        };

        request.onsuccess = event => {
            db = event.target.result;
            db.closePending = false;
            
            // Handle database closure (e.g., when another tab upgrades the version)
            db.onversionchange = () => {
                console.warn('Database version change detected. Closing connection.');
                db.closePending = true;
                db.close();
                db = null;
                dbPromise = null;
            };

            // Handle unexpected connection loss
            db.onclose = () => {
                console.warn('Database connection closed unexpectedly.');
                db = null;
                dbPromise = null;
            };
            
            resolve(db);
        };

        request.onerror = event => {
            const error = event.target.error;
            console.error('IndexedDB error:', error);
            reject(new Error(`Failed to open database: ${error?.message || 'Unknown error'}`));
        };

        request.onblocked = () => {
            console.warn('Database opening blocked. Close other tabs and try again.');
            // Don't reject, just warn - the request will eventually succeed or fail
        };
    });
}

/**
 * Creates a timeout promise
 * @param {number} ms Milliseconds to wait
 * @param {string} message Error message
 * @returns {Promise<never>}
 */
function createTimeout(ms, message) {
    return new Promise((_, reject) => {
        setTimeout(() => reject(new Error(message)), ms);
    });
}

/**
 * Adds or updates a record in the store (upsert operation)
 * @param {string} storeName The object store name
 * @param {any} record The record to add/update
 * @returns {Promise<string>} Success message
 */
export async function addRecord(storeName, record) {
    return executeTransaction(storeName, 'readwrite', store => store.put(record), 'addRecord');
}

/**
 * Updates a record in the store (alias for addRecord)
 * @param {string} storeName The object store name
 * @param {any} record The record to update
 * @returns {Promise<string>} Success message
 */
export async function updateRecord(storeName, record) {
    return executeTransaction(storeName, 'readwrite', store => store.put(record), 'updateRecord');
}

/**
 * Gets a single record by its key
 * @param {string} storeName The object store name
 * @param {any} key The record key
 * @returns {Promise<any|null>} The record or null if not found
 */
export async function getRecord(storeName, key) {
    const result = await executeTransaction(
        storeName, 
        'readonly', 
        store => store.get(key), 
        'getRecord',
        true // return result
    );
    return result ?? null;
}

/**
 * Gets all records from a store
 * @param {string} storeName The object store name
 * @returns {Promise<Array>} Array of all records
 */
export async function getRecords(storeName) {
    const result = await executeTransaction(
        storeName, 
        'readonly', 
        store => store.getAll(), 
        'getRecords',
        true // return result
    );
    return result || [];
}

/**
 * Deletes a record by its key
 * @param {string} storeName The object store name
 * @param {any} key The record key to delete
 * @returns {Promise<string>} Success message
 */
export async function deleteRecord(storeName, key) {
    return executeTransaction(storeName, 'readwrite', store => store.delete(key), 'deleteRecord');
}

/**
 * Deletes all records from a store
 * @param {string} storeName The object store name
 * @returns {Promise<string>} Success message
 */
export async function clearStore(storeName) {
    return executeTransaction(storeName, 'readwrite', store => store.clear(), 'clearStore');
}

/**
 * Generic transaction executor - DRY principle
 * @param {string} storeName The object store name
 * @param {IDBTransactionMode} mode Transaction mode ('readonly' or 'readwrite')
 * @param {Function} operation Function that performs the store operation
 * @param {string} operationName Name for logging
 * @param {boolean} returnResult Whether to return the operation result
 * @returns {Promise<any>}
 */
async function executeTransaction(storeName, mode, operation, operationName, returnResult = false) {
    try {
        const database = await ensureDbOpen();
        
        return new Promise((resolve, reject) => {
            let operationResult;
            
            try {
                const transaction = database.transaction([storeName], mode);
                const store = transaction.objectStore(storeName);
                const request = operation(store);

                if (returnResult) {
                    request.onsuccess = event => {
                        operationResult = event.target.result;
                    };
                }

                transaction.oncomplete = () => {
                    resolve(returnResult ? operationResult : `${operationName} completed successfully`);
                };

                transaction.onerror = event => {
                    const error = event.target.error;
                    console.error(`Transaction error in ${operationName}:`, error);
                    reject(new Error(`Transaction failed: ${error?.message || 'Unknown error'}`));
                };

                transaction.onabort = event => {
                    const error = event.target.error;
                    console.error(`Transaction aborted in ${operationName}:`, error);
                    reject(new Error(`Transaction aborted: ${error?.message || 'Unknown error'}`));
                };

                request.onerror = event => {
                    // Transaction will handle the error
                    event.preventDefault();
                };
            } catch (err) {
                reject(err);
            }
        });
    } catch (error) {
        console.error(`Error in ${operationName}:`, error);
        throw error;
    }
}

/**
 * Closes the database connection
 * Useful for cleanup, testing, or forcing reconnection
 */
export function closeDb() {
    if (db && !db.closePending) {
        db.closePending = true;
        db.close();
    }
    db = null;
    dbPromise = null;
}

/**
 * Gets database statistics
 * @returns {Promise<Object>} Database info including size and record counts
 */
export async function getDbInfo() {
    try {
        const database = await ensureDbOpen();
        const storeNames = Array.from(database.objectStoreNames);
        const info = {
            name: database.name,
            version: database.version,
            stores: []
        };

        for (const storeName of storeNames) {
            const count = await executeTransaction(
                storeName,
                'readonly',
                store => store.count(),
                'getDbInfo',
                true
            );
            info.stores.push({ name: storeName, recordCount: count });
        }

        return info;
    } catch (error) {
        console.error('Error getting database info:', error);
        throw error;
    }
}
